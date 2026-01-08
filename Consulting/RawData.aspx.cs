using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Data.SqlClient;
using System.Threading;
using HtmlAgilityPack;

public partial class Consulting_RawData : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void btnSubmit_Click(object sender, EventArgs e)
    {
        lblStatus.Text = string.Empty;
        var url = (txtSourceUrl.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            lblStatus.Text = "Vui lòng nhập đường link.";
            return;
        }

        try
        {
            var folder = Server.MapPath("~/App_Data/RawData");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var pageFrom = ParseInt(txtPageFrom.Text, 1);
            var pageTo = ParseInt(txtPageTo.Text, pageFrom);
            if (pageTo < pageFrom)
            {
                var temp = pageFrom;
                pageFrom = pageTo;
                pageTo = temp;
            }

            var province = (txtProvince.Text ?? string.Empty).Trim();
            var results = new List<RawContact>();

            var rand = new Random();
            for (var page = pageFrom; page <= pageTo; page++)
            {
                var pageUrl = BuildPageUrl(url, page);
                var fileName = "rawdata_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + page + ".html";
                var filePath = Path.Combine(folder, fileName);
                DownloadToFile(pageUrl, filePath);
                results.AddRange(ParseHtmlFile(filePath, pageUrl, province));

                if (page < pageTo)
                {
                    // Throttle between requests to reduce server load.
                    Thread.Sleep(rand.Next(5000, 10001));
                }
            }

            EnrichPhones(results);
            EnsureRawContactsTable();
            SaveContacts(results, province, url);
            rptResults.DataSource = results;
            rptResults.DataBind();

            lblStatus.Text = "Đã tải HTML: " + results.Count + " công ty.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi tải HTML: " + ex.Message;
        }
    }

    protected void btnClear_Click(object sender, EventArgs e)
    {
        txtProvince.Text = string.Empty;
        txtSourceUrl.Text = string.Empty;
        txtPageFrom.Text = string.Empty;
        txtPageTo.Text = string.Empty;
        lblStatus.Text = string.Empty;
    }

    private void DownloadToFile(string url, string filePath)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        request.Headers.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Referer = url;
        request.Timeout = 30000;

        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("Không nhận được dữ liệu từ server.");
            }
            stream.CopyTo(fileStream);
        }
    }

    private string BuildPageUrl(string templateUrl, int page)
    {
        if (string.IsNullOrWhiteSpace(templateUrl)) return templateUrl;
        if (templateUrl.Contains("{0}"))
        {
            return string.Format(templateUrl, page);
        }
        return templateUrl;
    }

    private int ParseInt(string value, int fallback)
    {
        int parsed;
        return int.TryParse(value, out parsed) ? parsed : fallback;
    }

    private List<RawContact> ParseHtmlFile(string filePath, string sourceUrl, string province)
    {
        var items = new List<RawContact>();
        var doc = new HtmlDocument();
        doc.Load(filePath, Encoding.UTF8);

        var listingNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'tax-listing')]/div[h3/a]");
        if (listingNodes != null && listingNodes.Count > 0)
        {
            foreach (var node in listingNodes)
            {
                var nameNode = node.SelectSingleNode(".//h3/a");
                var taxNode = node.SelectSingleNode(".//div[i[contains(@class,'fa-hashtag')]]//a[1]");
                var repNode = node.SelectSingleNode(".//div[i[contains(@class,'fa-user')]]//em/a");
                var addrNode = node.SelectSingleNode(".//address");

                var name = CleanText(nameNode != null ? nameNode.InnerText : string.Empty);
                var tax = CleanText(taxNode != null ? taxNode.InnerText : string.Empty);
                var rep = CleanText(repNode != null ? repNode.InnerText : string.Empty);
                var addr = CleanText(addrNode != null ? addrNode.InnerText : string.Empty);
                var detailUrl = NormalizeUrl(sourceUrl, nameNode != null ? nameNode.GetAttributeValue("href", string.Empty) : string.Empty);

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(tax)) continue;

                items.Add(new RawContact
                {
                    CompanyName = name,
                    TaxCode = tax,
                    Representative = rep,
                    Address = addr,
                    ProvinceFromAddress = ExtractProvinceFromAddress(addr),
                    DetailUrl = detailUrl,
                    Province = province
                });
            }
            return items;
        }

        var h1Node = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'h1')]");
        var taxNodeSingle = doc.DocumentNode.SelectSingleNode("//td[@itemprop='taxID']//span") ?? doc.DocumentNode.SelectSingleNode("//td[@itemprop='taxID']");
        var addrNodeSingle = doc.DocumentNode.SelectSingleNode("//td[@itemprop='address']//span") ?? doc.DocumentNode.SelectSingleNode("//td[@itemprop='address']");
        var repNodeSingle = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'tax-listing')]//i[contains(@class,'fa-user')]/following-sibling::em[1]//a");
        var canonical = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");

        var title = CleanText(h1Node != null ? h1Node.InnerText : string.Empty);
        var companyName = ExtractCompanyNameFromTitle(title);

        items.Add(new RawContact
        {
            CompanyName = companyName,
            TaxCode = CleanText(taxNodeSingle != null ? taxNodeSingle.InnerText : string.Empty),
            Representative = CleanText(repNodeSingle != null ? repNodeSingle.InnerText : string.Empty),
            Address = CleanText(addrNodeSingle != null ? addrNodeSingle.InnerText : string.Empty),
            ProvinceFromAddress = ExtractProvinceFromAddress(CleanText(addrNodeSingle != null ? addrNodeSingle.InnerText : string.Empty)),
            DetailUrl = canonical != null ? canonical.GetAttributeValue("href", sourceUrl) : sourceUrl,
            Province = province
        });
        return items;
    }

    private void EnrichPhones(List<RawContact> items)
    {
        if (items == null) return;
        var rand = new Random();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.DetailUrl)) continue;
            if (!string.IsNullOrWhiteSpace(item.Phone)) continue;
            try
            {
                for (var attempt = 0; attempt < 2 && string.IsNullOrWhiteSpace(item.Phone); attempt++)
                {
                    var html = DownloadHtml(item.DetailUrl);
                    if (string.IsNullOrWhiteSpace(html)) continue;
                    SaveDetailHtml(item, html);
                    var phone = ExtractPhoneFromDetail(html);
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        item.Phone = phone;
                        break;
                    }
                    if (IsLikelyBlocked(html))
                    {
                        Thread.Sleep(rand.Next(5000, 10001));
                    }
                }
            }
            catch
            {
                // Ignore per-item errors to keep batch running.
            }
            Thread.Sleep(rand.Next(1000, 3001));
        }
    }

    private string DownloadHtml(string url)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8";
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        request.Headers.Add("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Referer = url;
        request.Timeout = 30000;
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private string ExtractPhoneFromDetail(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var itempropTel = doc.DocumentNode.SelectSingleNode("//*[@itemprop='telephone']");
        if (itempropTel != null)
        {
            var tel = itempropTel.GetAttributeValue("content", string.Empty);
            if (string.IsNullOrWhiteSpace(tel))
            {
                var span = itempropTel.SelectSingleNode(".//span[contains(@class,'copy')]");
                tel = span != null ? span.InnerText : itempropTel.InnerText;
            }
            tel = CleanPhone(tel);
            if (!string.IsNullOrWhiteSpace(tel)) return tel;
        }

        var telNode = doc.DocumentNode.SelectSingleNode("//a[starts-with(@href,'tel:')]");
        if (telNode != null)
        {
            var tel = telNode.GetAttributeValue("href", string.Empty).Replace("tel:", "");
            if (!string.IsNullOrWhiteSpace(tel)) return CleanPhone(tel);
        }

        var phoneLabel = doc.DocumentNode.SelectSingleNode("//*[contains(translate(text(),'ĐIỆN THOẠI','điện thoại'),'điện thoại')]");
        if (phoneLabel != null)
        {
            var td = phoneLabel.ParentNode != null ? phoneLabel.ParentNode.SelectSingleNode("./following-sibling::td[1]") : null;
            var span = td != null ? td.SelectSingleNode(".//span") : null;
            var text = span != null ? span.InnerText : (td != null ? td.InnerText : string.Empty);
            text = CleanPhone(text);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        var tablePhone = doc.DocumentNode.SelectSingleNode("//td[contains(translate(.,'ĐIỆN THOẠI','điện thoại'),'điện thoại')]/following-sibling::td[1]");
        if (tablePhone != null)
        {
            var text = CleanPhone(tablePhone.InnerText);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        return string.Empty;
    }

    private bool IsLikelyBlocked(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return true;
        return html.IndexOf("table-taxinfo", StringComparison.OrdinalIgnoreCase) < 0
            && html.IndexOf("itemprop='taxID'", StringComparison.OrdinalIgnoreCase) < 0
            && html.IndexOf("itemprop=\"taxID\"", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private void SaveDetailHtml(RawContact item, string html)
    {
        if (item == null || string.IsNullOrWhiteSpace(html)) return;
        var folder = Server.MapPath("~/App_Data/RawData/Detail");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var key = item.TaxCode;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
        var fileName = "detail_" + SanitizeFileName(key) + ".html";
        var filePath = Path.Combine(folder, fileName);
        File.WriteAllText(filePath, html, Encoding.UTF8);
    }

    private string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "unknown";
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(ch, '_');
        }
        return input;
    }

    private void EnsureRawContactsTable()
    {
        var sql = @"
IF OBJECT_ID('dbo.cf_raw_contacts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.cf_raw_contacts (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        province NVARCHAR(100) NULL,
        company_name NVARCHAR(255) NULL,
        tax_code NVARCHAR(50) NULL,
        representative NVARCHAR(255) NULL,
        phone NVARCHAR(100) NULL,
        address NVARCHAR(500) NULL,
        province_from_address NVARCHAR(100) NULL,
        detail_url NVARCHAR(500) NULL,
        source_url NVARCHAR(500) NULL,
        created_at DATETIME NOT NULL CONSTRAINT DF_cf_raw_contacts_created_at DEFAULT (GETDATE())
    );
    CREATE INDEX IX_cf_raw_contacts_tax_code ON dbo.cf_raw_contacts(tax_code);
END;
IF COL_LENGTH('dbo.cf_raw_contacts', 'phone') IS NULL
BEGIN
    ALTER TABLE dbo.cf_raw_contacts ADD phone NVARCHAR(100) NULL;
END;
IF COL_LENGTH('dbo.cf_raw_contacts', 'province_from_address') IS NULL
BEGIN
    ALTER TABLE dbo.cf_raw_contacts ADD province_from_address NVARCHAR(100) NULL;
END;";
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                cmd.ExecuteNonQuery();
            }
        }
    }

    private void SaveContacts(List<RawContact> items, string province, string sourceUrl)
    {
        if (items == null || items.Count == 0) return;
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            foreach (var item in items)
            {
                var sql = @"
IF NOT EXISTS (
    SELECT 1 FROM dbo.cf_raw_contacts
    WHERE ISNULL(tax_code, '') = @taxCode AND ISNULL(detail_url, '') = @detailUrl
)
BEGIN
    INSERT INTO dbo.cf_raw_contacts
        (province, company_name, tax_code, representative, phone, address, province_from_address, detail_url, source_url)
    VALUES
        (@province, @companyName, @taxCode, @representative, @phone, @address, @provinceFromAddress, @detailUrl, @sourceUrl);
END;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.AddWithValue("@province", (object)(province ?? string.Empty));
                    cmd.Parameters.AddWithValue("@companyName", (object)(item.CompanyName ?? string.Empty));
                    cmd.Parameters.AddWithValue("@taxCode", (object)(item.TaxCode ?? string.Empty));
                    cmd.Parameters.AddWithValue("@representative", (object)(item.Representative ?? string.Empty));
                    cmd.Parameters.AddWithValue("@phone", (object)(item.Phone ?? string.Empty));
                    cmd.Parameters.AddWithValue("@address", (object)(item.Address ?? string.Empty));
                    cmd.Parameters.AddWithValue("@provinceFromAddress", (object)(item.ProvinceFromAddress ?? string.Empty));
                    cmd.Parameters.AddWithValue("@detailUrl", (object)(item.DetailUrl ?? string.Empty));
                    cmd.Parameters.AddWithValue("@sourceUrl", (object)(sourceUrl ?? string.Empty));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    private string NormalizeUrl(string sourceUrl, string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return sourceUrl;
        if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return href;
        try
        {
            var baseUri = new Uri(sourceUrl);
            return new Uri(baseUri, href).ToString();
        }
        catch
        {
            return href;
        }
    }

    private string ExtractCompanyNameFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var parts = title.Split(new[] { '-' }, 2);
        if (parts.Length == 2)
        {
            return CleanText(parts[1]);
        }
        return CleanText(title);
    }

    private string CleanText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return HttpUtility.HtmlDecode(input).Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
    }

    private string CleanPhone(string input)
    {
        var text = CleanText(input);
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Replace("Ẩn số điện thoại", string.Empty).Trim();
        return text;
    }

    private string ExtractProvinceFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        var normalized = address.ToLowerInvariant();
        var idx = normalized.LastIndexOf("việt nam");
        if (idx < 0) idx = normalized.LastIndexOf("viet nam");
        var before = idx >= 0 ? address.Substring(0, idx) : address;
        before = before.Trim().TrimEnd(',', '-', ';');
        if (string.IsNullOrWhiteSpace(before)) return string.Empty;
        var parts = before.Split(',');
        if (parts.Length == 0) return string.Empty;
        return parts[parts.Length - 1].Trim();
    }

    private class RawContact
    {
        public string Province { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string Representative { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string ProvinceFromAddress { get; set; }
        public string DetailUrl { get; set; }
    }
}

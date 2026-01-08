using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using Ionic.Zip;

public partial class CustomersTraveledExport : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            Response.StatusCode = 401;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        var role = Session["Role"] as string;
        if (string.IsNullOrWhiteSpace(role) || !role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = 403;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        string passport = (Request["passport"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string birthMonthStr = (Request["birthMonth"] ?? string.Empty).Trim();
        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();
        string tripFromStr = (Request["tripFrom"] ?? string.Empty).Trim();
        string tripToStr = (Request["tripTo"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var where = "WHERE c.visible = 1 AND LTRIM(RTRIM(ISNULL(c.passport,''))) <> ''";
        if (!string.IsNullOrWhiteSpace(passport))
        {
            where += " AND LTRIM(RTRIM(c.passport)) LIKE @passport";
            parameters.Add(new SqlParameter("@passport", "%" + passport + "%"));
        }
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            where += " AND LOWER(ISNULL(c.LastName,'') + ' ' + ISNULL(c.Firstname,'')) LIKE @customerName";
            parameters.Add(new SqlParameter("@customerName", "%" + customerName.ToLowerInvariant() + "%"));
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            where += " AND LTRIM(RTRIM(ISNULL(c.tel,''))) LIKE @phone";
            parameters.Add(new SqlParameter("@phone", "%" + phone + "%"));
        }
        int birthMonth;
        if (int.TryParse(birthMonthStr, out birthMonth) && birthMonth >= 1 && birthMonth <= 12)
        {
            where += " AND CASE WHEN c.birthday LIKE '__/__/____' AND ISNUMERIC(SUBSTRING(c.birthday,4,2)) = 1 THEN CAST(SUBSTRING(c.birthday,4,2) AS int) END = @birthMonth";
            parameters.Add(new SqlParameter("@birthMonth", birthMonth));
        }
        DateTime fromDt;
        DateTime toDt;
        if (!DateTime.TryParse(fromDate, out fromDt))
        {
            fromDt = DateTime.Today.AddMonths(-6);
        }
        if (!DateTime.TryParse(toDate, out toDt))
        {
            toDt = DateTime.Today;
        }
        where += " AND EXISTS (SELECT 1 FROM product p WHERE p.id = c.ProductID AND ISDATE(p.ngayKhoiHanh) = 1 AND CONVERT(date, p.ngayKhoiHanh, 103) >= @fromDate)";
        where += " AND EXISTS (SELECT 1 FROM product p WHERE p.id = c.ProductID AND ISDATE(p.ngayKhoiHanh) = 1 AND CONVERT(date, p.ngayKhoiHanh, 103) < @toDate)";
        parameters.Add(new SqlParameter("@fromDate", fromDt.Date));
        parameters.Add(new SqlParameter("@toDate", toDt.Date.AddDays(1)));
        int tripFrom;
        int tripTo;
        bool hasTripFrom = int.TryParse(tripFromStr, out tripFrom) && tripFrom > 0;
        bool hasTripTo = int.TryParse(tripToStr, out tripTo) && tripTo > 0;
        if (hasTripFrom) parameters.Add(new SqlParameter("@tripFrom", tripFrom));
        if (hasTripTo) parameters.Add(new SqlParameter("@tripTo", tripTo));

        var sql = @"
;WITH base AS (
    SELECT c.Id,
           LTRIM(RTRIM(c.passport)) AS Passport,
           COALESCE(NULLIF(LTRIM(RTRIM(c.LastName)), ''), LTRIM(RTRIM(o.ctLastname))) AS LastName,
           COALESCE(NULLIF(LTRIM(RTRIM(c.Firstname)), ''), LTRIM(RTRIM(o.ctFirstname))) AS Firstname,
           c.gender,
           c.birthday,
           LTRIM(RTRIM(c.tel)) AS tel,
           c.email,
           c.nationallity,
           c.Creationdate,
           c.ProductID,
           c.orderID
    FROM customer c
    LEFT JOIN [order] o ON o.OrderID = c.orderID
    " + where + @"
),
filtered AS (
    SELECT *,
           LOWER(LTRIM(RTRIM(ISNULL(LastName,''))) + ' ' + LTRIM(RTRIM(ISNULL(Firstname,'')))) AS NameKey,
           ISNULL(LTRIM(RTRIM(birthday)), '') AS BirthdayKey
    FROM base
),
latest_contact AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY NameKey, BirthdayKey ORDER BY Creationdate DESC, Id DESC) AS rn
    FROM filtered
),
phones AS (
    SELECT s.NameKey, s.BirthdayKey,
           STUFF((
               SELECT ';' + s3.Phone + '||' + s3.Source
               FROM (
                   SELECT Phone,
                          Source,
                          ROW_NUMBER() OVER (PARTITION BY NameKey, BirthdayKey, PhoneClean ORDER BY SourcePriority, Phone) AS rn
                   FROM (
                       SELECT f.NameKey,
                              f.BirthdayKey,
                              LTRIM(RTRIM(ISNULL(f.tel,''))) AS Phone,
                              CASE
                                  WHEN LEFT(pn.PhoneClean, 2) = '84' AND LEN(pn.PhoneClean) > 9 THEN '0' + RIGHT(pn.PhoneClean, LEN(pn.PhoneClean) - 2)
                                  ELSE pn.PhoneClean
                              END AS PhoneClean,
                              'customer' AS Source,
                              0 AS SourcePriority
                       FROM filtered f
                       CROSS APPLY (
                           SELECT REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(f.tel,''))),' ',''),'-',''),'.',''),'+','') AS PhoneClean
                       ) pn
                       WHERE ISNULL(f.tel,'') <> ''
                       UNION ALL
                       SELECT f.NameKey,
                              f.BirthdayKey,
                              LTRIM(RTRIM(ISNULL(o.ctTel,''))) AS Phone,
                              CASE
                                  WHEN LEFT(pn.PhoneClean, 2) = '84' AND LEN(pn.PhoneClean) > 9 THEN '0' + RIGHT(pn.PhoneClean, LEN(pn.PhoneClean) - 2)
                                  ELSE pn.PhoneClean
                              END AS PhoneClean,
                              'order' AS Source,
                              1 AS SourcePriority
                       FROM filtered f
                       JOIN [order] o ON o.OrderID = f.orderID
                       CROSS APPLY (
                           SELECT REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(o.ctTel,''))),' ',''),'-',''),'.',''),'+','') AS PhoneClean
                       ) pn
                       WHERE ISNULL(o.ctTel,'') <> ''
                   ) s2
                   WHERE s2.NameKey = s.NameKey AND s2.BirthdayKey = s.BirthdayKey
               ) s3
               WHERE s3.rn = 1
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 1, '') AS Phones
    FROM (
        SELECT DISTINCT NameKey, BirthdayKey FROM filtered
    ) s
),
passports AS (
    SELECT f.NameKey, f.BirthdayKey,
           STUFF((
               SELECT DISTINCT ';' + f2.Passport
               FROM filtered f2
               WHERE f2.NameKey = f.NameKey AND f2.BirthdayKey = f.BirthdayKey AND ISNULL(f2.Passport,'') <> ''
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 1, '') AS Passports
    FROM filtered f
    GROUP BY f.NameKey, f.BirthdayKey
),
latest_tour AS (
    SELECT
        f.NameKey,
        f.BirthdayKey,
        p.ngayKhoiHanh AS LatestDeparture,
        p.code AS Code,
        ROW_NUMBER() OVER (PARTITION BY f.NameKey, f.BirthdayKey ORDER BY p.ngayKhoiHanh DESC, f.Creationdate DESC, f.Id DESC) AS rn
    FROM filtered f
    JOIN product p ON p.id = f.ProductID
    WHERE p.ngayKhoiHanh IS NOT NULL
),
trip_counts AS (
    SELECT f.NameKey, f.BirthdayKey,
           COUNT(*) AS TripCount
    FROM filtered f
    GROUP BY f.NameKey, f.BirthdayKey
),
countries AS (
    SELECT f.NameKey, f.BirthdayKey,
           STUFF((
               SELECT DISTINCT ', ' + d.name
               FROM filtered f2
               JOIN [product-des] pd ON pd.productID = f2.ProductID
               JOIN destination d ON d.id = pd.destinationID
               WHERE f2.NameKey = f.NameKey AND f2.BirthdayKey = f.BirthdayKey
                 AND ISNULL(d.name,'') <> ''
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
    FROM filtered f
    GROUP BY f.NameKey, f.BirthdayKey
)
SELECT TOP (@maxRows)
    lc.Passport,
    lc.LastName,
    lc.Firstname,
    lc.gender,
    lc.birthday,
    ph.Phones,
    ps.Passports,
    lt.LatestDeparture,
    lt.Code,
    tc.TripCount,
    cn.Countries
FROM latest_contact lc
LEFT JOIN phones ph ON ph.NameKey = lc.NameKey AND ph.BirthdayKey = lc.BirthdayKey
LEFT JOIN passports ps ON ps.NameKey = lc.NameKey AND ps.BirthdayKey = lc.BirthdayKey
LEFT JOIN latest_tour lt ON lt.NameKey = lc.NameKey AND lt.BirthdayKey = lc.BirthdayKey AND lt.rn = 1
LEFT JOIN trip_counts tc ON tc.NameKey = lc.NameKey AND tc.BirthdayKey = lc.BirthdayKey
LEFT JOIN countries cn ON cn.NameKey = lc.NameKey AND cn.BirthdayKey = lc.BirthdayKey
WHERE lc.rn = 1
  AND (@tripFrom IS NULL OR tc.TripCount >= @tripFrom)
  AND (@tripTo IS NULL OR tc.TripCount <= @tripTo)
ORDER BY lt.LatestDeparture DESC, lc.Creationdate DESC
OPTION (RECOMPILE);";

        var table = new DataTable();
        table.Columns.Add("Passport");
        table.Columns.Add("Khách hàng");
        table.Columns.Add("Giới tính");
        table.Columns.Add("Ngày sinh");
        table.Columns.Add("Điện thoại");
        table.Columns.Add("Đã đi đến");
        table.Columns.Add("Số lần đi");
        table.Columns.Add("Lần đi gần nhất");

        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Math.Max(Db.CommandTimeoutSeconds, 600);
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                if (!hasTripFrom) cmd.Parameters.Add(new SqlParameter("@tripFrom", DBNull.Value));
                if (!hasTripTo) cmd.Parameters.Add(new SqlParameter("@tripTo", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@maxRows", 20000));
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var latestDate = reader["LatestDeparture"] != DBNull.Value
                            ? Convert.ToDateTime(reader["LatestDeparture"]).ToString("dd/MM/yyyy")
                            : "";
                        var latestCode = reader["Code"] as string ?? string.Empty;
                        var latestText = latestDate;
                        if (!string.IsNullOrEmpty(latestCode))
                        {
                            latestText = string.IsNullOrEmpty(latestDate) ? latestCode : latestDate + "\n" + latestCode;
                        }

                        var row = table.NewRow();
                        row["Passport"] = reader["Passports"] as string;
                        row["Khách hàng"] = BuildCustomerName(reader["LastName"] as string, reader["Firstname"] as string);
                        row["Giới tính"] = NormalizeGender(reader["gender"] as string);
                        row["Ngày sinh"] = reader["birthday"] as string;
                        row["Điện thoại"] = reader["Phones"] as string;
                        row["Đã đi đến"] = reader["Countries"] as string;
                        row["Số lần đi"] = reader["TripCount"] != DBNull.Value ? reader["TripCount"].ToString() : "0";
                        row["Lần đi gần nhất"] = latestText;
                        table.Rows.Add(row);
                    }
                }
            }
        }

        var rows = new List<List<string>>();
        var header = new List<string>();
        for (int i = 0; i < table.Columns.Count; i++)
        {
            header.Add(table.Columns[i].ColumnName);
        }
        rows.Add(header);

        foreach (DataRow dr in table.Rows)
        {
            var row = new List<string>();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var val = dr[i] != null ? dr[i].ToString() : string.Empty;
                row.Add(val);
            }
            rows.Add(row);
        }

        Response.Clear();
        Response.Buffer = true;
        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        Response.AddHeader("Content-Disposition", "attachment;filename=customers_traveled.xlsx");

        using (var zip = new ZipFile())
        {
            zip.AlternateEncodingUsage = ZipOption.AsNecessary;
            zip.AddEntry("[Content_Types].xml", GetContentTypesXml(), Encoding.UTF8);
            zip.AddEntry("_rels/.rels", GetRootRelsXml(), Encoding.UTF8);
            zip.AddEntry("xl/workbook.xml", GetWorkbookXml(), Encoding.UTF8);
            zip.AddEntry("xl/_rels/workbook.xml.rels", GetWorkbookRelsXml(), Encoding.UTF8);
            zip.AddEntry("xl/worksheets/sheet1.xml", GetSheetXml(rows), Encoding.UTF8);
            zip.Save(Response.OutputStream);
        }

        Response.Flush();
        HttpContext.Current.ApplicationInstance.CompleteRequest();
    }

    private string BuildCustomerName(string lastName, string firstName)
    {
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        return (lastName + " " + firstName).Trim();
    }

    private string NormalizeGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return gender;
        gender = gender.Trim();
        if (gender.Equals("F", StringComparison.OrdinalIgnoreCase)) return "Nữ";
        if (gender.Equals("M", StringComparison.OrdinalIgnoreCase)) return "Nam";
        return gender;
    }

    private string GetContentTypesXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
            + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
            + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
            + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
            + "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
            + "</Types>";
    }

    private string GetRootRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
            + "</Relationships>";
    }

    private string GetWorkbookXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" "
            + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
            + "<sheets><sheet name=\"CustomersTraveled\" sheetId=\"1\" r:id=\"rId1\"/></sheets>"
            + "</workbook>";
    }

    private string GetWorkbookRelsXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>"
            + "</Relationships>";
    }

    private string GetSheetXml(List<List<string>> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sb.Append("<sheetData>");
        for (int r = 0; r < rows.Count; r++)
        {
            int rowIndex = r + 1;
            sb.Append("<row r=\"").Append(rowIndex).Append("\">");
            for (int c = 0; c < rows[r].Count; c++)
            {
                string cellRef = GetExcelColumnName(c) + rowIndex;
                string value = EscapeXml(rows[r][c] ?? string.Empty);
                sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"><is><t>")
                    .Append(value)
                    .Append("</t></is></c>");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private string EscapeXml(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private string GetExcelColumnName(int index)
    {
        int dividend = index + 1;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }
}

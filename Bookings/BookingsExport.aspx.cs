using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using Ionic.Zip;

public partial class BookingsExport : System.Web.UI.Page
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

        string orderId = (Request["orderId"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string source = (Request["source"] ?? string.Empty).Trim();
        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();

        var excludeNames = new[] { "tour leader", "first name", "star travel", "summer uyen" };
        var parameters = new List<SqlParameter>();
        var where = "WHERE o.Visible = 1";
        where += " AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname,'')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname,'')))) NOT IN (@ex1,@ex2,@ex3,@ex4)";
        for (int i = 0; i < excludeNames.Length; i++)
        {
            parameters.Add(new SqlParameter("@ex" + (i + 1), excludeNames[i]));
        }

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            where += " AND CAST(o.orderid AS NVARCHAR(50)) LIKE @orderId";
            parameters.Add(new SqlParameter("@orderId", "%" + orderId + "%"));
        }
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            where += " AND LOWER(ISNULL(o.ctLastname,'') + ' ' + ISNULL(o.ctFirstname,'')) LIKE @customerName";
            parameters.Add(new SqlParameter("@customerName", "%" + customerName.ToLowerInvariant() + "%"));
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            where += " AND o.ctTel LIKE @phone";
            parameters.Add(new SqlParameter("@phone", "%" + phone + "%"));
        }
        if (!string.IsNullOrWhiteSpace(source))
        {
            where += " AND o.ctnguon LIKE @source";
            parameters.Add(new SqlParameter("@source", "%" + source + "%"));
        }
        DateTime fromDt;
        if (DateTime.TryParse(fromDate, out fromDt))
        {
            where += " AND o.Creationdate >= @fromDate";
            parameters.Add(new SqlParameter("@fromDate", fromDt.Date));
        }
        DateTime toDt;
        if (DateTime.TryParse(toDate, out toDt))
        {
            where += " AND o.Creationdate < @toDate";
            parameters.Add(new SqlParameter("@toDate", toDt.Date.AddDays(1)));
        }

        var sql = @"
SELECT
    o.orderid AS Booking,
    tourcode.TourCode,
    tour.DepartureDate,
    cust.CountCustomers,
    LTRIM(RTRIM(ISNULL(o.ctLastname,''))) + ' ' + LTRIM(RTRIM(ISNULL(o.ctFirstname,''))) AS CustomerName,
    o.ctgender AS Gender,
    o.ctTel AS Phone,
    prod.Countries,
    o.Amountthucban,
    o.DepositDeadline,
    u.username AS CreatedBy,
    o.Creationdate
FROM [order] o
LEFT JOIN users u ON u.id = o.userid
OUTER APPLY (
    SELECT COUNT(*) AS CountCustomers FROM customer c0 WHERE c0.orderID = o.orderid AND c0.Visible = 1
) cust
OUTER APPLY (
    SELECT MIN(p.ngaydi) AS DepartureDate
    FROM customer c1
    LEFT JOIN price p ON p.id = c1.IDPrice
    WHERE c1.orderID = o.orderid AND c1.Visible = 1
) tour
OUTER APPLY (
    SELECT TOP 1 p.Code AS TourCode
    FROM customer c2
    INNER JOIN product p ON p.id = c2.ProductID
    WHERE c2.orderID = o.orderid AND c2.Visible = 1 AND ISNULL(p.Code, '') <> ''
    ORDER BY c2.Creationdate DESC, c2.Id DESC
) tourcode
OUTER APPLY (
    SELECT TOP 1 STUFF((
        SELECT DISTINCT ', ' + d.name
        FROM customer c
        JOIN [product-des] pd ON pd.productID = c.ProductID
        JOIN destination d ON d.id = pd.destinationID
        WHERE c.orderID = o.orderid AND ISNULL(d.name,'') <> ''
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
) prod
" + where + @"
ORDER BY o.Creationdate DESC, o.orderid DESC;";

        var table = new DataTable();
        table.Columns.Add("Booking");
        table.Columns.Add("Tour");
        table.Columns.Add("Ngày khởi hành");
        table.Columns.Add("Số khách");
        table.Columns.Add("Khách hàng");
        table.Columns.Add("Giới tính");
        table.Columns.Add("Điện thoại");
        table.Columns.Add("Quốc gia");
        table.Columns.Add("Thực bán");
        table.Columns.Add("Hạn thanh toán");
        table.Columns.Add("Người tạo");
        table.Columns.Add("Ngày tạo");

        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = table.NewRow();
                        row["Booking"] = reader["Booking"];
                        row["Tour"] = reader["TourCode"] as string;
                        row["Ngày khởi hành"] = reader["DepartureDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["DepartureDate"]).ToString("dd/MM/yyyy")
                            : "";
                        row["Số khách"] = reader["CountCustomers"] != DBNull.Value ? reader["CountCustomers"].ToString() : "0";
                        row["Khách hàng"] = reader["CustomerName"] as string;
                        row["Giới tính"] = NormalizeGender(reader["Gender"] as string);
                        row["Điện thoại"] = reader["Phone"] as string;
                        row["Quốc gia"] = reader["Countries"] as string;
                        row["Thực bán"] = reader["Amountthucban"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Amountthucban"]).ToString("N0")
                            : "";
                        row["Hạn thanh toán"] = reader["DepositDeadline"] != DBNull.Value
                            ? Convert.ToDateTime(reader["DepositDeadline"]).ToString("dd/MM/yyyy")
                            : "";
                        row["Người tạo"] = reader["CreatedBy"] as string;
                        row["Ngày tạo"] = reader["Creationdate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Creationdate"]).ToString("dd/MM/yyyy")
                            : "";
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
        Response.AddHeader("Content-Disposition", "attachment;filename=bookings_export.xlsx");

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
            + "<sheets><sheet name=\"Bookings\" sheetId=\"1\" r:id=\"rId1\"/></sheets>"
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

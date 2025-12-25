using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Web;

public partial class CustomersExport : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var role = Session["Role"] as string;
        bool isAdmin = !string.IsNullOrEmpty(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin)
        {
            Response.StatusCode = 403;
            Response.Write("Forbidden");
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        string phone = (Request["phone"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string fromDateStr = (Request["fromDate"] ?? string.Empty).Trim();
        string toDateStr = (Request["toDate"] ?? string.Empty).Trim();

        var excludeNames = new[] { "nikkie nhung", "le duong", "pham thi minh chau", "tour leader", "star travel", "summer uyen" };
        var parameters = new List<SqlParameter>();

        string cleanTelExpr = "REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(o.ctTel,''),' ',''),'-',''),'.',''),'+','')";
        var baseWhere = "WHERE o.Visible = 1 AND " + cleanTelExpr + " <> ''";
        baseWhere += " AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname,'')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname,'')))) NOT IN (@ex1,@ex2,@ex3,@ex4,@ex5,@ex6)";
        for (int i = 0; i < excludeNames.Length; i++)
        {
            parameters.Add(new SqlParameter("@ex" + (i + 1), excludeNames[i]));
        }
        var where = baseWhere;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            where += " AND LTRIM(RTRIM(o.ctTel)) LIKE @phone";
            parameters.Add(new SqlParameter("@phone", "%" + phone + "%"));
        }
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            where += " AND LOWER(ISNULL(o.ctLastname,'') + ' ' + ISNULL(o.ctFirstname,'')) LIKE @customerName";
            parameters.Add(new SqlParameter("@customerName", "%" + customerName.ToLowerInvariant() + "%"));
        }
        DateTime fromDate;
        if (DateTime.TryParse(fromDateStr, out fromDate))
        {
            where += " AND CONVERT(date, o.Creationdate) >= @fromDate";
            parameters.Add(new SqlParameter("@fromDate", fromDate.Date));
        }
        DateTime toDate;
        if (DateTime.TryParse(toDateStr, out toDate))
        {
            where += " AND CONVERT(date, o.Creationdate) <= @toDate";
            parameters.Add(new SqlParameter("@toDate", toDate.Date));
        }

        var sql = @"
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
;WITH base AS (
    SELECT o.orderid,
           o.ctTel,
           " + cleanTelExpr + @" AS CleanTel,
           o.ctLastname,
           o.ctFirstname,
           o.ctgender,
           o.ctnguon,
           o.Creationdate,
           o.Amountthucban
    FROM [order] o
    " + where + @"
),
agg AS (
    SELECT CleanTel, COUNT(*) AS TotalBookings, MAX(Creationdate) AS LatestCreation, SUM(ISNULL(Amountthucban,0)) AS TotalAmountThucBan
    FROM base
    GROUP BY CleanTel
),
latest AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY CleanTel ORDER BY Creationdate DESC, orderid DESC) AS rn
    FROM base
)
SELECT 
    l.ctTel,
    l.ctLastname,
    l.ctFirstname,
    l.ctgender,
    a.TotalBookings,
    a.TotalAmountThucBan,
    prod.Countries AS ProductName,
    dep.DepartureDates
FROM agg a
JOIN latest l ON l.CleanTel = a.CleanTel AND l.rn = 1
OUTER APPLY (
    SELECT TOP 1
        CASE 
            WHEN dests.Countries IS NOT NULL AND dests.Countries <> '' THEN dests.Countries
            ELSE prodNames.Names
        END AS Countries
    FROM (
        SELECT STUFF((
            SELECT DISTINCT ', ' + d.name
            FROM base b
            JOIN customer c ON c.orderID = b.orderid
            JOIN [product-des] pd ON pd.productID = c.ProductID
            JOIN destination d ON d.id = pd.destinationID
            WHERE b.CleanTel = a.CleanTel AND ISNULL(d.name, '') <> ''
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
    ) dests
    CROSS APPLY (
        SELECT STUFF((
            SELECT DISTINCT ', ' + ISNULL(p.name, '')
            FROM base b2
            JOIN customer c2 ON c2.orderID = b2.orderid
            JOIN product p ON p.id = c2.ProductID
            WHERE b2.CleanTel = a.CleanTel AND ISNULL(p.name,'') <> ''
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, '') AS Names
    ) prodNames
) prod
OUTER APPLY (
    SELECT STUFF((
        SELECT DISTINCT ', ' + CONVERT(varchar(10), p.ngayKhoiHanh, 103)
        FROM base b3
        JOIN customer c3 ON c3.orderID = b3.orderid
        JOIN product p ON p.id = c3.ProductID
        WHERE b3.CleanTel = a.CleanTel AND p.ngayKhoiHanh IS NOT NULL
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS DepartureDates
) dep
ORDER BY a.LatestCreation DESC, a.TotalBookings DESC, l.ctTel OPTION (RECOMPILE);";

        Response.Clear();
        Response.Buffer = true;
        Response.ContentType = "text/csv";
        Response.Charset = "utf-8";
        Response.ContentEncoding = Encoding.UTF8;
        Response.AddHeader("Content-Disposition", "attachment; filename=customers.csv");
        // BOM for Excel UTF-8
        Response.BinaryWrite(Encoding.UTF8.GetPreamble());

        var sb = new StringBuilder();
        sb.AppendLine("Phone,CustomerName,Gender,Countries,TotalBookings,TotalAmount,DepartureDates");

        using (var conn = Db.CreateConnection())
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.CommandTimeout = Db.CommandTimeoutSeconds;
            foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string phoneVal = reader["ctTel"] as string;
                    string nameVal = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string);
                    string genderVal = NormalizeGender(reader["ctgender"] as string);
                    string countries = reader["ProductName"] as string;
                    string totalBookings = reader["TotalBookings"] != DBNull.Value ? Convert.ToInt32(reader["TotalBookings"]).ToString() : "0";
                    string totalAmount = reader["TotalAmountThucBan"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmountThucBan"]).ToString() : "0";
                    string departureDates = reader["DepartureDates"] as string;
                    sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        CsvEscape(phoneVal),
                        CsvEscape(nameVal),
                        CsvEscape(genderVal),
                        CsvEscape(countries),
                        CsvEscape(totalBookings),
                        CsvEscape(totalAmount),
                        CsvEscape(departureDates)
                    ));
                }
            }
        }

        Response.Write(sb.ToString());
        HttpContext.Current.ApplicationInstance.CompleteRequest();
    }

    private string CsvEscape(string input)
    {
        if (input == null) return "\"\"";
        input = input.Replace("\"", "\"\"");
        return "\"" + input + "\"";
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
}

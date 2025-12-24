using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CustomersApi : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            Response.StatusCode = 401;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        Response.ContentType = "application/json";
        Response.TrySkipIisCustomErrors = true;
        Response.Clear();
        Response.Buffer = true;
        Response.Charset = "utf-8";

        var drawStr = Request["draw"] ?? "1";
        int draw = ParseInt(drawStr, 1);
        int start = ParseInt(Request["start"], 0);
        int length = ParseInt(Request["length"], 50);
        if (length <= 0) length = 50;
        if (length > 500) length = 500;

        string phone = (Request["phone"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string fromDateStr = (Request["fromDate"] ?? string.Empty).Trim();
        string toDateStr = (Request["toDate"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var excludeNames = new[] { "nikkie nhung", "le duong", "pham thi minh chau", "tour leader", "star travel", "summer uyen" };

        var cleanTelExpr = "REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(o.ctTel,''),' ',''),'-',''),'.',''),'+','')";
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

        try
        {
            long total = 0;
            long filtered = 0;
            var data = new List<Dictionary<string, object>>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand("SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SELECT COUNT(DISTINCT " + cleanTelExpr + ") FROM [order] o " + baseWhere, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                using (var countFilteredCmd = new SqlCommand("SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SELECT COUNT(*) FROM (SELECT DISTINCT " + cleanTelExpr + " AS CleanTel FROM [order] o " + where + ") t", conn))
                {
                    countFilteredCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countFilteredCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    filtered = Convert.ToInt64(countFilteredCmd.ExecuteScalar());
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
    a.LatestCreation,
    latestDeparture.LatestDeparture,
    latestDeparture.LatestCode,
    a.TotalAmountThucBan,
    prod.Countries AS ProductName
FROM agg a
JOIN latest l ON l.CleanTel = a.CleanTel AND l.rn = 1
OUTER APPLY (
    SELECT MAX(p.ngayKhoiHanh) AS LatestDeparture,
           MAX(p.code) AS LatestCode
    FROM customer c3
    JOIN product p ON p.id = c3.ProductID
    WHERE c3.orderID = l.orderid AND p.ngayKhoiHanh IS NOT NULL
) latestDeparture
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
ORDER BY a.LatestCreation DESC, a.TotalBookings DESC, l.ctTel
OFFSET @start ROWS FETCH NEXT @length ROWS ONLY OPTION (RECOMPILE);";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@start", start));
                    cmd.Parameters.Add(new SqlParameter("@length", length));
                    foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["Phone"] = reader["ctTel"];
                            row["CustomerName"] = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string);
                            row["Gender"] = NormalizeGender(reader["ctgender"] as string);
                            row["ProductName"] = reader["ProductName"] as string;
                            row["TotalBookings"] = reader["TotalBookings"];
                            row["LatestCreation"] = reader["LatestCreation"];
                            row["LatestDeparture"] = reader["LatestDeparture"] != DBNull.Value
                                ? Convert.ToDateTime(reader["LatestDeparture"]).ToString("dd/MM/yyyy")
                                : "";
                            row["LatestCode"] = reader["LatestCode"] as string;
                            row["TotalAmountThucBan"] = reader["TotalAmountThucBan"];
                            data.Add(row);
                        }
                    }
                }
            }

            var result = new
            {
                draw = draw,
                recordsTotal = total,
                recordsFiltered = filtered,
                data = data
            };
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            Response.Write(serializer.Serialize(result));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            Response.StatusCode = 200;
            Response.Write(serializer.Serialize(new { error = ex.Message, data = new object[0], recordsTotal = 0, recordsFiltered = 0 }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
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
    }}



using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CustomersDetailApi : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            Response.StatusCode = 401;
            Response.End();
            return;
        }

        Response.ContentType = "application/json";
        Response.TrySkipIisCustomErrors = true;
        Response.Clear();

        string phone = (Request["phone"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            WriteJson(new { error = "Thiếu số điện thoại" });
            return;
        }

        try
        {
            string cleanTelExpr = "REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(o.ctTel,''),' ',''),'-',''),'.',''),'+','')";
            var parameters = new List<SqlParameter>();
            var excludeNames = new[] { "nikkie nhung", "le duong", "pham thi minh chau", "tour leader", "star travel", "summer uyen" };
            var where = "WHERE o.Visible = 1 AND " + cleanTelExpr + " <> ''";
            where += " AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname,'')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname,'')))) NOT IN (@ex1,@ex2,@ex3,@ex4,@ex5,@ex6)";
            for (int i = 0; i < excludeNames.Length; i++)
            {
                parameters.Add(new SqlParameter("@ex" + (i + 1), excludeNames[i]));
            }
            where += " AND LTRIM(RTRIM(o.ctTel)) LIKE @phoneLike";
            parameters.Add(new SqlParameter("@phoneLike", phone + "%"));

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
SELECT TOP 1
    l.ctTel,
    l.CleanTel,
    l.ctLastname,
    l.ctFirstname,
    l.ctgender,
    a.TotalBookings,
    a.TotalAmountThucBan,
    prod.Countries AS ProductName
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
) prod;";

            string cleanTelFound = null;
            var result = new Dictionary<string, object>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result["Phone"] = reader["ctTel"] as string;
                            cleanTelFound = reader["CleanTel"] as string;
                            result["CustomerName"] = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string);
                            result["Gender"] = NormalizeGender(reader["ctgender"] as string);
                            result["TotalBookings"] = reader["TotalBookings"];
                            result["TotalAmountThucBan"] = reader["TotalAmountThucBan"];
                            result["ProductName"] = reader["ProductName"] as string;
                        }
                        else
                        {
                            WriteJson(new { error = "Không tìm thấy khách hàng" });
                            return;
                        }
                    }
                }

                if (string.IsNullOrEmpty(cleanTelFound))
                {
                    cleanTelFound = NormalizePhone(phone);
                }

                var orders = new List<Dictionary<string, object>>();
                using (var orderCmd = new SqlCommand(@"
SELECT TOP 50
    o.orderid,
    o.Creationdate,
    o.ctnguon,
    o.Amountthucban,
    ISNULL(guestCounts.TotalGuests, 0) AS NumGuests,
    u.username AS CreatedBy,
    prodCodes.CodeTour,
    prodCountries.Countries
FROM [order] o
LEFT JOIN users u ON u.id = o.userid
OUTER APPLY (
    SELECT COUNT(*) AS TotalGuests
    FROM customer cg
    WHERE cg.orderID = o.orderid
) guestCounts
OUTER APPLY (
    SELECT TOP 1 ISNULL(p.code, '') AS CodeTour
    FROM customer c
    JOIN product p ON p.id = c.ProductID
    WHERE c.orderID = o.orderid AND ISNULL(p.code,'') <> ''
) prodCodes
OUTER APPLY (
    SELECT TOP 1 STUFF((
        SELECT DISTINCT ', ' + d.name
        FROM customer c2
        JOIN [product-des] pd ON pd.productID = c2.ProductID
        JOIN destination d ON d.id = pd.destinationID
        WHERE c2.orderID = o.orderid AND ISNULL(d.name,'') <> ''
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
) prodCountries
WHERE " + cleanTelExpr + @" = @cleanTel AND o.Visible = 1
ORDER BY o.Creationdate DESC, o.orderid DESC;", conn))
                {
                    orderCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    orderCmd.Parameters.Add(new SqlParameter("@cleanTel", cleanTelFound));
                    using (var reader = orderCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["OrderId"] = reader["orderid"];
                            row["CreationDate"] = reader["Creationdate"] != DBNull.Value ? Convert.ToDateTime(reader["Creationdate"]).ToString("dd/MM/yyyy") : "";
                            row["Source"] = reader["ctnguon"] as string;
                            row["NumGuests"] = reader["NumGuests"] != DBNull.Value ? reader["NumGuests"].ToString() : "";
                            row["CreatedBy"] = NormalizeUsername(reader["CreatedBy"] as string);
                            row["ProductCode"] = reader["CodeTour"] as string;
                            row["Countries"] = reader["Countries"] as string;
                            row["AmountThucBan"] = reader["Amountthucban"];
                            orders.Add(row);
                        }
                    }
                }

                result["Orders"] = orders;
            }

            WriteJson(result);
        }
        catch (Exception ex)
        {
            WriteJson(new { error = ex.Message });
        }
    }

    private void WriteJson(object obj)
    {
        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;
        Response.Write(serializer.Serialize(obj));
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

    private string NormalizePhone(string phone)
    {
        if (phone == null) return string.Empty;
        return phone.Replace(" ", "").Replace("-", "").Replace(".", "").Replace("+", "");
    }

    private string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return username;
        var at = username.IndexOf('@');
        if (at >= 0) return username.Substring(0, at);
        return username;
    }
}

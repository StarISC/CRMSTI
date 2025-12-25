using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CustomersTraveledApi : System.Web.UI.Page
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

        int draw = ParseInt(Request["draw"], 1);
        int start = ParseInt(Request["start"], 0);
        int length = ParseInt(Request["length"], 50);
        if (length <= 0) length = 50;
        if (length > 500) length = 500;

        string passport = (Request["passport"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string birthMonthStr = (Request["birthMonth"] ?? string.Empty).Trim();

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

        try
        {
            long total = 0;
            long filtered = 0;
            var data = new List<Dictionary<string, object>>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand("SELECT COUNT(DISTINCT LTRIM(RTRIM(c.passport))) FROM customer c " + where, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                filtered = total;

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
           c.ProductID
    FROM customer c
    LEFT JOIN [order] o ON o.OrderID = c.orderID
    " + where + @"
),
latest_contact AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY Passport ORDER BY Creationdate DESC, Id DESC) AS rn
    FROM base
),
page_list AS (
    SELECT Passport, LastName, Firstname, gender, birthday, tel, email, nationallity, Creationdate
    FROM latest_contact
    WHERE rn = 1
    ORDER BY Creationdate DESC, Passport
    OFFSET @start ROWS FETCH NEXT @length ROWS ONLY
),
phones AS (
    SELECT s.Passport,
           STUFF((
               SELECT ';' + s3.Phone + '||' + s3.Source
               FROM (
                   SELECT Phone,
                          Source,
                          ROW_NUMBER() OVER (PARTITION BY Passport, PhoneClean ORDER BY SourcePriority, Phone) AS rn
                   FROM (
                       SELECT b.Passport,
                              LTRIM(RTRIM(ISNULL(b.tel,''))) AS Phone,
                              CASE
                                  WHEN LEFT(pn.PhoneClean, 2) = '84' AND LEN(pn.PhoneClean) > 9 THEN '0' + RIGHT(pn.PhoneClean, LEN(pn.PhoneClean) - 2)
                                  ELSE pn.PhoneClean
                              END AS PhoneClean,
                              'customer' AS Source,
                              0 AS SourcePriority
                       FROM base b
                       CROSS APPLY (
                           SELECT REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(b.tel,''))),' ',''),'-',''),'.',''),'+','') AS PhoneClean
                       ) pn
                       WHERE ISNULL(b.tel,'') <> ''
                       UNION ALL
                       SELECT LTRIM(RTRIM(c.passport)) AS Passport,
                              LTRIM(RTRIM(ISNULL(o.ctTel,''))) AS Phone,
                              CASE
                                  WHEN LEFT(pn.PhoneClean, 2) = '84' AND LEN(pn.PhoneClean) > 9 THEN '0' + RIGHT(pn.PhoneClean, LEN(pn.PhoneClean) - 2)
                                  ELSE pn.PhoneClean
                              END AS PhoneClean,
                              'order' AS Source,
                              1 AS SourcePriority
                       FROM customer c
                       JOIN [order] o ON o.OrderID = c.orderID
                       CROSS APPLY (
                           SELECT REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(o.ctTel,''))),' ',''),'-',''),'.',''),'+','') AS PhoneClean
                       ) pn
                       WHERE c.visible = 1
                         AND LTRIM(RTRIM(ISNULL(c.passport,''))) <> ''
                         AND ISNULL(o.ctTel,'') <> ''
                   ) s2
                   WHERE s2.Passport = s.Passport
               ) s3
               WHERE s3.rn = 1
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 1, '') AS Phones
    FROM (
        SELECT DISTINCT LTRIM(RTRIM(c.passport)) AS Passport
        FROM customer c
        WHERE c.visible = 1
          AND LTRIM(RTRIM(ISNULL(c.passport,''))) <> ''
    ) s
),
latest_tour AS (
    SELECT
        b.Passport,
        p.ngayKhoiHanh AS LatestDeparture,
        p.code AS Code,
        ROW_NUMBER() OVER (PARTITION BY b.Passport ORDER BY p.ngayKhoiHanh DESC, b.Creationdate DESC, b.Id DESC) AS rn
    FROM base b
    JOIN product p ON p.id = b.ProductID
    WHERE p.ngayKhoiHanh IS NOT NULL
),
trip_counts AS (
    SELECT b.Passport,
           COUNT(*) AS TripCount
    FROM base b
    GROUP BY b.Passport
),
countries AS (
    SELECT b.Passport,
           STUFF((
               SELECT DISTINCT ', ' + d.name
               FROM base b2
               JOIN [product-des] pd ON pd.productID = b2.ProductID
               JOIN destination d ON d.id = pd.destinationID
               WHERE b2.Passport = b.Passport
                 AND ISNULL(d.name,'') <> ''
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
    FROM base b
    GROUP BY b.Passport
)
SELECT 
    p.Passport,
    p.LastName,
    p.Firstname,
    p.gender,
    p.birthday,
    ph.Phones,
    p.email,
    p.nationallity,
    lt.LatestDeparture,
    lt.Code,
    tc.TripCount,
    cn.Countries
FROM page_list p
LEFT JOIN phones ph ON ph.Passport = p.Passport
LEFT JOIN latest_tour lt ON lt.Passport = p.Passport AND lt.rn = 1
LEFT JOIN trip_counts tc ON tc.Passport = p.Passport
LEFT JOIN countries cn ON cn.Passport = p.Passport
ORDER BY lt.LatestDeparture DESC, p.Creationdate DESC, p.Passport
OPTION (RECOMPILE);";

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
                            row["Passport"] = reader["Passport"] as string;
                            row["CustomerName"] = BuildCustomerName(reader["LastName"] as string, reader["Firstname"] as string);
                            row["Gender"] = NormalizeGender(reader["gender"] as string);
                            row["Birthday"] = reader["birthday"] as string;
                            row["Phone"] = reader["Phones"] as string;
                            row["Nationality"] = reader["nationallity"] as string;
                            row["LatestCreation"] = reader["LatestDeparture"] != DBNull.Value
                                ? Convert.ToDateTime(reader["LatestDeparture"]).ToString("dd/MM/yyyy")
                                : "";
                            row["LatestCode"] = reader["Code"] as string;
                            row["TripCount"] = reader["TripCount"] != DBNull.Value
                                ? Convert.ToInt32(reader["TripCount"]).ToString()
                                : "0";
                            row["Countries"] = reader["Countries"] as string;
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
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private int ParseInt(string value, int fallback)
    {
        int result;
        return int.TryParse(value, out result) ? result : fallback;
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

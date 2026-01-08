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
        if (DateTime.TryParse(fromDate, out fromDt))
        {
            where += " AND EXISTS (SELECT 1 FROM product p WHERE p.id = c.ProductID AND ISDATE(p.ngayKhoiHanh) = 1 AND CONVERT(date, p.ngayKhoiHanh, 103) >= @fromDate)";
            parameters.Add(new SqlParameter("@fromDate", fromDt.Date));
        }
        DateTime toDt;
        if (DateTime.TryParse(toDate, out toDt))
        {
            where += " AND EXISTS (SELECT 1 FROM product p WHERE p.id = c.ProductID AND ISDATE(p.ngayKhoiHanh) = 1 AND CONVERT(date, p.ngayKhoiHanh, 103) < @toDate)";
            parameters.Add(new SqlParameter("@toDate", toDt.Date.AddDays(1)));
        }
        int tripFrom;
        int tripTo;
        bool hasTripFrom = int.TryParse(tripFromStr, out tripFrom) && tripFrom > 0;
        bool hasTripTo = int.TryParse(tripToStr, out tripTo) && tripTo > 0;
        if (hasTripFrom) parameters.Add(new SqlParameter("@tripFrom", tripFrom));
        if (hasTripTo) parameters.Add(new SqlParameter("@tripTo", tripTo));

        try
        {
            long total = 0;
            long filtered = 0;
            var data = new List<Dictionary<string, object>>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                var countSql = @"
;WITH base AS (
    SELECT c.Id,
           COALESCE(NULLIF(LTRIM(RTRIM(c.LastName)), ''), LTRIM(RTRIM(o.ctLastname))) AS LastName,
           COALESCE(NULLIF(LTRIM(RTRIM(c.Firstname)), ''), LTRIM(RTRIM(o.ctFirstname))) AS Firstname,
           c.birthday,
           c.Creationdate,
           c.ProductID
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
trip_counts AS (
    SELECT NameKey, BirthdayKey, COUNT(*) AS TripCount
    FROM filtered
    GROUP BY NameKey, BirthdayKey
)
SELECT COUNT(*)
FROM trip_counts tc
WHERE (@tripFrom IS NULL OR tc.TripCount >= @tripFrom)
  AND (@tripTo IS NULL OR tc.TripCount <= @tripTo);";

                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    if (!hasTripFrom) countCmd.Parameters.Add(new SqlParameter("@tripFrom", DBNull.Value));
                    if (!hasTripTo) countCmd.Parameters.Add(new SqlParameter("@tripTo", DBNull.Value));
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
page_list AS (
    SELECT Passport, LastName, Firstname, gender, birthday, tel, email, nationallity, Creationdate, NameKey, BirthdayKey
    FROM latest_contact
    WHERE rn = 1
    ORDER BY Creationdate DESC, NameKey
    OFFSET @start ROWS FETCH NEXT @length ROWS ONLY
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
SELECT 
    p.Passport,
    p.LastName,
    p.Firstname,
    p.gender,
    p.birthday,
    ph.Phones,
    ps.Passports,
    p.email,
    p.nationallity,
    lt.LatestDeparture,
    lt.Code,
    tc.TripCount,
    cn.Countries
FROM page_list p
LEFT JOIN phones ph ON ph.NameKey = p.NameKey AND ph.BirthdayKey = p.BirthdayKey
LEFT JOIN passports ps ON ps.NameKey = p.NameKey AND ps.BirthdayKey = p.BirthdayKey
LEFT JOIN latest_tour lt ON lt.NameKey = p.NameKey AND lt.BirthdayKey = p.BirthdayKey AND lt.rn = 1
LEFT JOIN trip_counts tc ON tc.NameKey = p.NameKey AND tc.BirthdayKey = p.BirthdayKey
LEFT JOIN countries cn ON cn.NameKey = p.NameKey AND cn.BirthdayKey = p.BirthdayKey
WHERE (@tripFrom IS NULL OR tc.TripCount >= @tripFrom)
  AND (@tripTo IS NULL OR tc.TripCount <= @tripTo)
ORDER BY lt.LatestDeparture DESC, p.Creationdate DESC
OPTION (RECOMPILE);";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@start", start));
                    cmd.Parameters.Add(new SqlParameter("@length", length));
                    foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    if (!hasTripFrom) cmd.Parameters.Add(new SqlParameter("@tripFrom", DBNull.Value));
                    if (!hasTripTo) cmd.Parameters.Add(new SqlParameter("@tripTo", DBNull.Value));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["Passport"] = reader["Passport"] as string;
                            row["Passports"] = reader["Passports"] as string;
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

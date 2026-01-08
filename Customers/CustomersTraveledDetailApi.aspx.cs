using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CustomersTraveledDetailApi : System.Web.UI.Page
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

        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string birthday = (Request["birthday"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(customerName))
        {
            WriteJson(new { error = "Thiếu thông tin khách hàng" });
            return;
        }

        try
        {
            var result = new Dictionary<string, object>();
            var infoSql = @"
;WITH base AS (
    SELECT
        LTRIM(RTRIM(c.passport)) AS Passport,
        COALESCE(NULLIF(LTRIM(RTRIM(c.LastName)), ''), LTRIM(RTRIM(o.ctLastname))) AS LastName,
        COALESCE(NULLIF(LTRIM(RTRIM(c.Firstname)), ''), LTRIM(RTRIM(o.ctFirstname))) AS Firstname,
        c.gender,
        c.birthday,
        COALESCE(NULLIF(LTRIM(RTRIM(c.tel)), ''), LTRIM(RTRIM(o.ctTel))) AS Phone,
        c.ProductID,
        c.Creationdate
    FROM customer c
    LEFT JOIN [order] o ON o.OrderID = c.orderID
    WHERE c.visible = 1
),
filtered AS (
    SELECT *,
           LOWER(LTRIM(RTRIM(ISNULL(LastName,''))) + ' ' + LTRIM(RTRIM(ISNULL(Firstname,'')))) AS NameKey,
           ISNULL(LTRIM(RTRIM(birthday)), '') AS BirthdayKey
    FROM base
),
latest_contact AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY NameKey, BirthdayKey ORDER BY Creationdate DESC) AS rn
    FROM filtered
),
trip_counts AS (
    SELECT NameKey, BirthdayKey, COUNT(*) AS TripCount
    FROM filtered
    GROUP BY NameKey, BirthdayKey
),
latest_tour AS (
    SELECT f.NameKey, f.BirthdayKey,
           p.ngayKhoiHanh AS LatestDeparture,
           p.code AS LatestCode,
           ROW_NUMBER() OVER (PARTITION BY f.NameKey, f.BirthdayKey ORDER BY p.ngayKhoiHanh DESC, f.Creationdate DESC) AS rn
    FROM filtered f
    JOIN product p ON p.id = f.ProductID
    WHERE p.ngayKhoiHanh IS NOT NULL
),
countries AS (
    SELECT f.NameKey, f.BirthdayKey,
           STUFF((
               SELECT DISTINCT ', ' + d.name
               FROM filtered f2
               JOIN [product-des] pd ON pd.productID = f2.ProductID
               JOIN destination d ON d.id = pd.destinationID
               WHERE f2.NameKey = f.NameKey AND f2.BirthdayKey = f.BirthdayKey AND ISNULL(d.name,'') <> ''
               FOR XML PATH(''), TYPE
           ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
    FROM filtered f
    GROUP BY f.NameKey, f.BirthdayKey
)
SELECT 
    l.Passport,
    l.LastName,
    l.Firstname,
    l.gender,
    l.birthday,
    l.Phone,
    tc.TripCount,
    lt.LatestDeparture,
    lt.LatestCode,
    cn.Countries
FROM latest_contact l
LEFT JOIN trip_counts tc ON tc.NameKey = l.NameKey AND tc.BirthdayKey = l.BirthdayKey
LEFT JOIN latest_tour lt ON lt.NameKey = l.NameKey AND lt.BirthdayKey = l.BirthdayKey AND lt.rn = 1
LEFT JOIN countries cn ON cn.NameKey = l.NameKey AND cn.BirthdayKey = l.BirthdayKey
WHERE l.rn = 1
  AND l.NameKey = @nameKey
  AND l.BirthdayKey = @birthdayKey;";

            var nameKey = customerName.Trim().ToLowerInvariant();
            var birthdayKey = birthday ?? string.Empty;

            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(infoSql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                cmd.Parameters.Add(new SqlParameter("@nameKey", nameKey));
                cmd.Parameters.Add(new SqlParameter("@birthdayKey", birthdayKey));
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result["Passport"] = reader["Passport"] as string;
                        result["CustomerName"] = BuildCustomerName(reader["LastName"] as string, reader["Firstname"] as string);
                        result["Gender"] = NormalizeGender(reader["gender"] as string);
                        result["Birthday"] = reader["birthday"] as string;
                        result["Phone"] = reader["Phone"] as string;
                        result["TripCount"] = reader["TripCount"] != DBNull.Value ? Convert.ToInt32(reader["TripCount"]).ToString() : "0";
                        result["LatestDeparture"] = reader["LatestDeparture"] != DBNull.Value
                            ? Convert.ToDateTime(reader["LatestDeparture"]).ToString("dd/MM/yyyy")
                            : "";
                        result["LatestCode"] = reader["LatestCode"] as string;
                        result["Countries"] = reader["Countries"] as string;
                    }
                }
            }

            var data = new List<Dictionary<string, object>>();
            var sql = @"
SELECT DISTINCT
    o.orderid,
    o.Amountthucban,
    o.DepositDeadline,
    o.ctTel,
    o.ctLastname,
    o.ctFirstname,
    p.id,
    p.code,
    p.ShipName,
    p.ngayKhoiHanh,
    countries.Countries,
    payments.TotalPaid
FROM customer c
JOIN [order] o ON o.OrderID = c.orderID
JOIN product p ON p.id = c.ProductID
OUTER APPLY (
    SELECT SUM(ISNULL(pmt.amount, 0)) AS TotalPaid
    FROM payment pmt
    WHERE pmt.orderID = o.OrderID AND pmt.visible = 1
) payments
OUTER APPLY (
    SELECT STUFF((
        SELECT DISTINCT ', ' + d.name
        FROM customer c2
        JOIN [product-des] pd ON pd.productID = c2.ProductID
        JOIN destination d ON d.id = pd.destinationID
        WHERE LOWER(LTRIM(RTRIM(ISNULL(c2.LastName,''))) + ' ' + LTRIM(RTRIM(ISNULL(c2.Firstname,'')))) = @nameKey
          AND ISNULL(LTRIM(RTRIM(c2.birthday)), '') = @birthdayKey
          AND c2.ProductID = p.id
          AND ISNULL(d.name,'') <> ''
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
) countries
WHERE c.visible = 1
  AND o.Visible = 1
  AND LOWER(LTRIM(RTRIM(ISNULL(c.LastName,''))) + ' ' + LTRIM(RTRIM(ISNULL(c.Firstname,'')))) = @nameKey
  AND ISNULL(LTRIM(RTRIM(c.birthday)), '') = @birthdayKey
ORDER BY p.ngayKhoiHanh DESC, p.id DESC;";

            using (var conn = Db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                cmd.Parameters.Add(new SqlParameter("@nameKey", nameKey));
                cmd.Parameters.Add(new SqlParameter("@birthdayKey", birthdayKey));
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        row["Code"] = reader["code"] as string;
                        row["ShipName"] = reader["ShipName"] as string;
                        row["DepartureDate"] = reader["ngayKhoiHanh"] != DBNull.Value
                            ? Convert.ToDateTime(reader["ngayKhoiHanh"]).ToString("dd/MM/yyyy")
                            : "";
                        row["Countries"] = reader["Countries"] as string;
                        row["Phone"] = reader["ctTel"] as string;
                        row["CreatedBy"] = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string);
                        row["Status"] = BuildStatus(reader["Amountthucban"], reader["TotalPaid"], reader["DepositDeadline"]);
                        data.Add(row);
                    }
                }
            }

            result["Orders"] = data;
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

    private string BuildStatus(object amountObj, object totalPaidObj, object depositDeadlineObj)
    {
        decimal amount = 0;
        decimal totalPaid = 0;
        DateTime? deadline = null;

        if (amountObj != DBNull.Value && amountObj != null)
        {
            amount = Convert.ToDecimal(amountObj);
        }

        if (totalPaidObj != DBNull.Value && totalPaidObj != null)
        {
            totalPaid = Convert.ToDecimal(totalPaidObj);
        }

        if (depositDeadlineObj != DBNull.Value && depositDeadlineObj != null)
        {
            deadline = Convert.ToDateTime(depositDeadlineObj);
        }

        if (amount > 0 && totalPaid >= amount)
        {
            return "FP";
        }

        if (totalPaid > 0 && totalPaid < amount)
        {
            return "BK";
        }

        if (totalPaid <= 0)
        {
            if (deadline.HasValue)
            {
                return deadline.Value.Date < DateTime.Today ? "CX" : "OP";
            }
            return "OP";
        }

        return string.Empty;
    }
}

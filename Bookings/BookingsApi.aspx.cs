using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class BookingsApi : System.Web.UI.Page
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

        string orderId = (Request["orderId"] ?? string.Empty).Trim();
        string customerName = (Request["customerName"] ?? string.Empty).Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string source = (Request["source"] ?? string.Empty).Trim();
        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();
        bool sortByAmount = Request["sort"] == "amount";

        var excludeNames = new[] { "tour leader", "first name", "star travel", "summer uyen" };
        var parameters = new List<SqlParameter>();
        var baseWhere = "WHERE o.Visible = 1";
        baseWhere += " AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname,'')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname,'')))) NOT IN (@ex1,@ex2,@ex3,@ex4)";
        for (int i = 0; i < excludeNames.Length; i++)
        {
            parameters.Add(new SqlParameter("@ex" + (i + 1), excludeNames[i]));
        }
        var where = baseWhere;
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

        long total = 0;
        long filtered = 0;
        var data = new List<Dictionary<string, object>>();

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM [order] o " + baseWhere, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                using (var countFilteredCmd = new SqlCommand("SELECT COUNT(*) FROM [order] o " + where, conn))
                {
                    countFilteredCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countFilteredCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    filtered = Convert.ToInt64(countFilteredCmd.ExecuteScalar());
                }

                var orderBy = sortByAmount
                    ? "o.Amount DESC, o.Creationdate DESC, o.orderid DESC"
                    : "o.Creationdate DESC, o.orderid DESC";

                var sql = @"
SELECT 
    o.orderid,
    o.ctLastname,
    o.ctFirstname,
    o.ctgender,
    o.ctTel,
    o.ctnguon,
    o.Creationdate,
    tourcode.TourCode,
    o.Amount,
    o.Amountthucban,
    o.DepositDeadline,
    u.username AS CreatedBy,
    prod.Countries,
    tour.DepartureDate,
    statusCalc.Status,
    cust.CountCustomers
FROM [order] o
LEFT JOIN users u ON u.id = o.userid
OUTER APPLY (
    SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
OUTER APPLY (
    SELECT CASE
        WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
            CASE
                WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                ELSE 'OP'
            END
        WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
        ELSE 'BK'
    END AS Status
) statusCalc
OUTER APPLY (
    SELECT COUNT(*) AS CountCustomers FROM customer c0 WHERE c0.orderID = o.orderid AND c0.Visible = 1
) cust
OUTER APPLY (
    SELECT TOP 1 p.Code AS TourCode
    FROM customer c2
    INNER JOIN product p ON p.id = c2.ProductID
    WHERE c2.orderID = o.orderid AND c2.Visible = 1 AND ISNULL(p.Code, '') <> ''
    ORDER BY c2.Creationdate DESC, c2.Id DESC
) tourcode
OUTER APPLY (
    SELECT MIN(p.ngaydi) AS DepartureDate
    FROM customer c1
    LEFT JOIN price p ON p.id = c1.IDPrice
    WHERE c1.orderID = o.orderid AND c1.Visible = 1
) tour
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
ORDER BY " + orderBy + @"
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
                            row["OrderId"] = reader["orderid"];
                            row["CustomerName"] = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string);
                            row["Gender"] = NormalizeGender(reader["ctgender"] as string);
                            row["Phone"] = reader["ctTel"];
                            row["Source"] = reader["ctnguon"];
                            row["ProductName"] = reader["Countries"] as string;
                            row["TourCode"] = reader["TourCode"] as string;
                            row["DepartureDate"] = reader["DepartureDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DepartureDate"]).ToString("dd/MM/yyyy")
                                : "";
                            row["Status"] = reader["Status"] as string;
                            row["CustomerCount"] = reader["CountCustomers"];
                            row["CreatedBy"] = NormalizeUsername(reader["CreatedBy"] as string);
                            row["CreationDate"] = reader["Creationdate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["Creationdate"]).ToString("dd/MM/yyyy")
                                : "";
                            row["Amount"] = reader["Amount"];
                            row["AmountThucBan"] = reader["Amountthucban"];
                            row["DepositDeadline"] = reader["DepositDeadline"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DepositDeadline"]).ToString("dd/MM/yyyy")
                                : "";
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

            Response.Write(serializer.Serialize(result));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
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
    }

    private string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return username;
        username = username.Trim();
        var at = username.IndexOf('@');
        if (at >= 0) return username.Substring(0, at);
        return username;
    }
}

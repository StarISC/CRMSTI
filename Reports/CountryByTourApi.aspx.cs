using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CountryByTourApi : System.Web.UI.Page
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

        string keyword = (Request["keyword"] ?? string.Empty).Trim();
        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var where = "WHERE o.Visible = 1";
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where += " AND d.name LIKE @keyword";
            parameters.Add(new SqlParameter("@keyword", "%" + keyword + "%"));
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

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        try
        {
            long total = 0;
            long filtered = 0;
            var data = new List<Dictionary<string, object>>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                var countSql = @"
;WITH order_base AS (
    SELECT
        o.orderid,
        o.Amount,
        o.Amountthucban
    FROM [order] o
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
    WHERE o.Visible = 1
      AND statusCalc.Status IN ('BK','FP')
),
order_country AS (
    SELECT DISTINCT
        ob.orderid,
        ob.Amount,
        ob.Amountthucban,
        ISNULL(NULLIF(LTRIM(RTRIM(d.name)), ''), N'Không rõ') AS CountryName
    FROM order_base ob
    JOIN customer c ON c.orderID = ob.orderid AND c.Visible = 1
    JOIN [product-des] pd ON pd.productID = c.ProductID
    JOIN destination d ON d.id = pd.destinationID
    JOIN [order] o ON o.orderid = ob.orderid
    " + where + @"
),
summary AS (
    SELECT CountryName
    FROM order_country
    GROUP BY CountryName
)
SELECT COUNT(*) FROM summary;";

                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                filtered = total;

                var sql = @"
;WITH order_base AS (
    SELECT
        o.orderid,
        o.Amount,
        o.Amountthucban
    FROM [order] o
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
    WHERE o.Visible = 1
      AND statusCalc.Status IN ('BK','FP')
),
order_country AS (
    SELECT DISTINCT
        ob.orderid,
        ob.Amount,
        ob.Amountthucban,
        ISNULL(NULLIF(LTRIM(RTRIM(d.name)), ''), N'Không rõ') AS CountryName
    FROM order_base ob
    JOIN customer c ON c.orderID = ob.orderid AND c.Visible = 1
    JOIN [product-des] pd ON pd.productID = c.ProductID
    JOIN destination d ON d.id = pd.destinationID
    JOIN [order] o ON o.orderid = ob.orderid
    " + where + @"
),
agg AS (
    SELECT
        CountryName,
        COUNT(DISTINCT orderid) AS TotalBookings,
        SUM(ISNULL(Amount, 0)) AS TotalAmount,
        SUM(ISNULL(Amountthucban, 0)) AS TotalAmountThucBan
    FROM order_country
    GROUP BY CountryName
),
guest_country AS (
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(d.name)), ''), N'Không rõ') AS CountryName,
        COUNT(*) AS TotalGuests
    FROM customer c
    JOIN [product-des] pd ON pd.productID = c.ProductID
    JOIN destination d ON d.id = pd.destinationID
    JOIN [order] o ON o.orderID = c.orderID
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
    " + where + @"
    AND statusCalc.Status IN ('BK','FP')
    AND c.Visible = 1
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(d.name)), ''), N'Không rõ')
)
SELECT
    a.CountryName,
    a.TotalBookings,
    ISNULL(g.TotalGuests, 0) AS TotalGuests,
    a.TotalAmount,
    a.TotalAmountThucBan
FROM agg a
LEFT JOIN guest_country g ON g.CountryName = a.CountryName
ORDER BY a.TotalAmountThucBan DESC, a.TotalBookings DESC
OFFSET @start ROWS FETCH NEXT @length ROWS ONLY
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
                            row["CountryName"] = reader["CountryName"] as string;
                            row["TotalBookings"] = reader["TotalBookings"];
                            row["TotalGuests"] = reader["TotalGuests"];
                            row["TotalAmount"] = reader["TotalAmount"];
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

            Response.Write(serializer.Serialize(result));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Write(serializer.Serialize(new { error = ex.Message, data = new object[0], recordsTotal = 0, recordsFiltered = 0 }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
    }
}

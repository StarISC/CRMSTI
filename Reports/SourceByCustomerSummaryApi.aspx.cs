using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class SourceByCustomerSummaryApi : System.Web.UI.Page
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

        string keyword = (Request["keyword"] ?? string.Empty).Trim();
        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var where = "WHERE o.Visible = 1";
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where += " AND o.ctnguon LIKE @keyword";
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
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                var sql = @"
;WITH base AS (
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(o.ctnguon)), ''), N'Không rõ') AS SourceName,
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
    " + where + @"
    AND statusCalc.Status IN ('BK','FP')
),
agg AS (
    SELECT
        SourceName,
        COUNT(*) AS TotalBookings,
        SUM(ISNULL(Amount, 0)) AS TotalAmount,
        SUM(ISNULL(Amountthucban, 0)) AS TotalAmountThucBan
    FROM base
    GROUP BY SourceName
),
guests AS (
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(o.ctnguon)), ''), N'Không rõ') AS SourceName,
        COUNT(*) AS TotalGuests
    FROM customer c
    JOIN [order] o ON o.OrderID = c.orderID
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
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(o.ctnguon)), ''), N'Không rõ')
)
SELECT
    COUNT(*) AS TotalSources,
    SUM(TotalBookings) AS TotalBookings,
    SUM(TotalAmountThucBan) AS TotalAmountThucBan,
    SUM(TotalAmount) AS TotalAmount,
    ISNULL((SELECT SUM(TotalGuests) FROM guests), 0) AS TotalGuests
FROM agg;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var result = new
                            {
                                TotalSources = reader["TotalSources"],
                                TotalBookings = reader["TotalBookings"],
                                TotalGuests = reader["TotalGuests"],
                                TotalAmountThucBan = reader["TotalAmountThucBan"],
                                TotalAmount = reader["TotalAmount"]
                            };
                            Response.Write(serializer.Serialize(result));
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }
            }

            Response.Write(serializer.Serialize(new { TotalSources = 0, TotalBookings = 0, TotalGuests = 0, TotalAmountThucBan = 0, TotalAmount = 0 }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}

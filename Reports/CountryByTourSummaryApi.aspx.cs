using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class CountryByTourSummaryApi : System.Web.UI.Page
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
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

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
    COUNT(*) AS TotalCountries,
    SUM(TotalBookings) AS TotalBookings,
    SUM(TotalAmountThucBan) AS TotalAmountThucBan,
    SUM(TotalAmount) AS TotalAmount,
    ISNULL((SELECT SUM(TotalGuests) FROM guest_country), 0) AS TotalGuests
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
                                TotalCountries = reader["TotalCountries"],
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

            Response.Write(serializer.Serialize(new { TotalCountries = 0, TotalBookings = 0, TotalGuests = 0, TotalAmountThucBan = 0, TotalAmount = 0 }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}

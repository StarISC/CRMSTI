using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class SalesByStaffSummaryApi : System.Web.UI.Page
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

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        string fromDate = (Request["fromDate"] ?? string.Empty).Trim();
        string toDate = (Request["toDate"] ?? string.Empty).Trim();
        string keyword = (Request["keyword"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var where = "WHERE o.Visible = 1 AND ISNULL(o.Amountthucban, 0) > 0 AND (u.Role = 'user' OR u.Role = 'DevAgent' OR u.username = @specialUser) AND statusCalc.Status IN ('BK','FP')";

        DateTime fromVal;
        if (DateTime.TryParse(fromDate, out fromVal))
        {
            where += " AND o.Creationdate >= @fromDate";
            parameters.Add(new SqlParameter("@fromDate", fromVal.Date));
        }

        DateTime toVal;
        if (DateTime.TryParse(toDate, out toVal))
        {
            where += " AND o.Creationdate < @toDate";
            parameters.Add(new SqlParameter("@toDate", toVal.Date.AddDays(1)));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where += " AND u.name LIKE @keyword";
            parameters.Add(new SqlParameter("@keyword", "%" + keyword + "%"));
        }

        parameters.Add(new SqlParameter("@specialUser", "vy.lephuong@startravelvn.com"));

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                var sql = @"
SELECT
    COUNT(DISTINCT o.userid) AS TotalStaff,
    COUNT(*) AS TotalBookings,
    SUM(ISNULL(cust.CountCustomers, 0)) AS TotalGuests,
    SUM(ISNULL(o.Amount, 0)) AS TotalAmount,
    SUM(ISNULL(o.Amountthucban, 0)) AS TotalAmountThucBan
FROM dbo.[order] o
INNER JOIN dbo.users u ON o.userid = u.id
OUTER APPLY (
    SELECT COUNT(*) AS CountCustomers
    FROM customer c
    WHERE c.orderID = o.orderid AND c.Visible = 1
) cust
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
" + where + ";";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var result = new Dictionary<string, object>();
                            result["TotalStaff"] = reader["TotalStaff"];
                            result["TotalBookings"] = reader["TotalBookings"];
                            result["TotalGuests"] = reader["TotalGuests"];
                            result["TotalAmount"] = reader["TotalAmount"];
                            result["TotalAmountThucBan"] = reader["TotalAmountThucBan"];
                            Response.Write(serializer.Serialize(result));
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }
            }

            Response.Write(serializer.Serialize(new { TotalStaff = 0, TotalBookings = 0, TotalGuests = 0, TotalAmount = 0, TotalAmountThucBan = 0 }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.StatusCode = 200;
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}

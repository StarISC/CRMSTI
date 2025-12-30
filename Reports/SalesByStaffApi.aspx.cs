using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class SalesByStaffApi : System.Web.UI.Page
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

        int draw = ParseInt(Request["draw"], 1);
        int start = ParseInt(Request["start"], 0);
        int length = ParseInt(Request["length"], 50);
        if (length <= 0) length = 50;
        if (length > 500) length = 500;

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

        long total = 0;
        long filtered = 0;
        var data = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand(@"
SELECT COUNT(DISTINCT o.userid)
FROM dbo.[order] o
INNER JOIN dbo.users u ON o.userid = u.id
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
" + where, conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                var countFilteredSql = @"
SELECT COUNT(DISTINCT o.userid)
FROM dbo.[order] o
INNER JOIN dbo.users u ON o.userid = u.id
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
" + where;
                using (var countFilteredCmd = new SqlCommand(countFilteredSql, conn))
                {
                    countFilteredCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countFilteredCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    filtered = Convert.ToInt64(countFilteredCmd.ExecuteScalar());
                }

                var sql = @"
SELECT u.id AS StaffId,
       u.name AS StaffName,
       u.IsShowReportCombo,
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
" + where + @"
GROUP BY u.id, u.name, u.IsShowReportCombo
ORDER BY u.IsShowReportCombo DESC, TotalBookings DESC
OFFSET @start ROWS FETCH NEXT @length ROWS ONLY;";

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
                            row["StaffName"] = reader["StaffName"] as string ?? "(Không rõ)";
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
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

public partial class _Default : System.Web.UI.Page
{
    private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            Response.Redirect("Login.aspx");
            return;
        }

        if (!IsPostBack)
        {
            LoadDashboard();
        }
    }

    private void LoadDashboard()
    {
        var summary = GetSummary();
        ltBookingsToday.Text = summary.BookingsToday.ToString("N0", VnCulture);
        ltBookingsWeek.Text = summary.BookingsWeek.ToString("N0", VnCulture);
        ltBookingsMonth.Text = summary.BookingsMonth.ToString("N0", VnCulture);
        ltRevenueMonth.Text = summary.RevenueMonth.ToString("N0", VnCulture);
        ltTotalCustomers.Text = summary.TotalCustomers.ToString("N0", VnCulture);

        ltStatusOP.Text = summary.StatusOP.ToString("N0", VnCulture);
        ltStatusCX.Text = summary.StatusCX.ToString("N0", VnCulture);
        ltStatusBK.Text = summary.StatusBK.ToString("N0", VnCulture);
        ltStatusFP.Text = summary.StatusFP.ToString("N0", VnCulture);

        rptSources.DataSource = GetTopSources();
        rptSources.DataBind();

        rptCreators.DataSource = GetTopCreators();
        rptCreators.DataBind();

        rptDeadlines.DataSource = GetUpcomingDeadlines();
        rptDeadlines.DataBind();

        rptTrend.DataSource = GetTrend();
        rptTrend.DataBind();
    }

    private DashboardSummary GetSummary()
    {
        var result = new DashboardSummary();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
SELECT
    SUM(CASE WHEN CONVERT(date, o.Creationdate) = @today THEN 1 ELSE 0 END) AS BookingsToday,
    SUM(CASE WHEN o.Creationdate >= DATEADD(day, -6, @today) THEN 1 ELSE 0 END) AS BookingsWeek,
    SUM(CASE WHEN o.Creationdate >= DATEFROMPARTS(YEAR(@today), MONTH(@today), 1) THEN 1 ELSE 0 END) AS BookingsMonth,
    SUM(CASE WHEN o.Creationdate >= DATEFROMPARTS(YEAR(@today), MONTH(@today), 1) THEN ISNULL(o.Amountthucban, 0) ELSE 0 END) AS RevenueMonth
FROM [order] o
WHERE o.Visible = 1;
";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.BookingsToday = SafeInt(reader["BookingsToday"]);
                        result.BookingsWeek = SafeInt(reader["BookingsWeek"]);
                        result.BookingsMonth = SafeInt(reader["BookingsMonth"]);
                        result.RevenueMonth = SafeDecimal(reader["RevenueMonth"]);
                    }
                }
            }

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM customer WHERE visible = 1", conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                result.TotalCustomers = Convert.ToInt32(cmd.ExecuteScalar());
            }

            var statusSql = @"
DECLARE @today date = CONVERT(date, GETDATE());
SELECT
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) = 0 AND o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) >= @today THEN 1 ELSE 0 END) AS OP,
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) = 0 AND o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < @today THEN 1 ELSE 0 END) AS CX,
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) > 0 AND ISNULL(o.Amountthucban, 0) > ISNULL(pay.TotalPaid, 0) THEN 1 ELSE 0 END) AS BK,
    SUM(CASE WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 1 ELSE 0 END) AS FP
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1;
";
            using (var cmd = new SqlCommand(statusSql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.StatusOP = SafeInt(reader["OP"]);
                        result.StatusCX = SafeInt(reader["CX"]);
                        result.StatusBK = SafeInt(reader["BK"]);
                        result.StatusFP = SafeInt(reader["FP"]);
                    }
                }
            }
        }
        return result;
    }

    private List<SourceStat> GetTopSources()
    {
        var items = new List<SourceStat>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
SELECT TOP 5
    ISNULL(NULLIF(LTRIM(RTRIM(ctnguon)), ''), '(Chưa có)') AS Source,
    COUNT(*) AS Total,
    SUM(ISNULL(Amountthucban, 0)) AS Revenue
FROM [order]
WHERE Visible = 1
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(ctnguon)), ''), '(Chưa có)')
ORDER BY Total DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SourceStat
                        {
                            Source = reader["Source"] as string,
                            Total = SafeInt(reader["Total"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
                        });
                    }
                }
            }
        }
        return items;
    }

    private List<CreatorStat> GetTopCreators()
    {
        var items = new List<CreatorStat>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
SELECT TOP 5
    ISNULL(NULLIF(u.username, ''), '(Không rõ)') AS CreatedBy,
    COUNT(*) AS Total,
    SUM(ISNULL(o.Amountthucban, 0)) AS Revenue
FROM [order] o
LEFT JOIN users u ON u.id = o.userid
WHERE o.Visible = 1
GROUP BY ISNULL(NULLIF(u.username, ''), '(Không rõ)')
ORDER BY Total DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new CreatorStat
                        {
                            CreatedBy = NormalizeUsername(reader["CreatedBy"] as string),
                            Total = SafeInt(reader["Total"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
                        });
                    }
                }
            }
        }
        return items;
    }

    private List<DeadlineItem> GetUpcomingDeadlines()
    {
        var items = new List<DeadlineItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
        var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
SELECT TOP 8
    o.orderid,
    o.ctLastname,
    o.ctFirstname,
    o.DepositDeadline
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1
  AND o.DepositDeadline IS NOT NULL
  AND CONVERT(date, o.DepositDeadline) BETWEEN @today AND DATEADD(day, 7, @today)
  AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname, '')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname, '')))) <> 'tour leader'
  AND ISNULL(pay.TotalPaid, 0) = 0
ORDER BY o.DepositDeadline ASC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new DeadlineItem
                        {
                            OrderId = reader["orderid"].ToString(),
                            CustomerName = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string),
                            DepositDeadline = reader["DepositDeadline"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DepositDeadline"]).ToString("dd/MM/yyyy")
                                : string.Empty
                        });
                    }
                }
            }
        }
        return items;
    }

    private List<TrendItem> GetTrend()
    {
        var items = new List<TrendItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
SELECT
    CONVERT(date, o.Creationdate) AS DayKey,
    COUNT(*) AS Total,
    SUM(ISNULL(o.Amountthucban, 0)) AS Revenue
FROM [order] o
WHERE o.Visible = 1
  AND o.Creationdate >= DATEADD(day, -6, @today)
GROUP BY CONVERT(date, o.Creationdate)
ORDER BY DayKey ASC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = reader["DayKey"] != DBNull.Value
                            ? Convert.ToDateTime(reader["DayKey"]).ToString("dd/MM/yyyy")
                            : string.Empty;
                        items.Add(new TrendItem
                        {
                            Date = date,
                            Total = SafeInt(reader["Total"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
                        });
                    }
                }
            }
        }
        return items;
    }

    private static int SafeInt(object value)
    {
        return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private static decimal SafeDecimal(object value)
    {
        return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
    }

    private string BuildCustomerName(string lastName, string firstName)
    {
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        return (lastName + " " + firstName).Trim();
    }

    private string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return username;
        username = username.Trim();
        var at = username.IndexOf('@');
        if (at >= 0) return username.Substring(0, at);
        return username;
    }

    private class DashboardSummary
    {
        public int BookingsToday { get; set; }
        public int BookingsWeek { get; set; }
        public int BookingsMonth { get; set; }
        public decimal RevenueMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int StatusOP { get; set; }
        public int StatusCX { get; set; }
        public int StatusBK { get; set; }
        public int StatusFP { get; set; }
    }

    private class SourceStat
    {
        public string Source { get; set; }
        public int Total { get; set; }
        public string Revenue { get; set; }
    }

    private class CreatorStat
    {
        public string CreatedBy { get; set; }
        public int Total { get; set; }
        public string Revenue { get; set; }
    }

    private class DeadlineItem
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string DepositDeadline { get; set; }
    }

    private class TrendItem
    {
        public string Date { get; set; }
        public int Total { get; set; }
        public string Revenue { get; set; }
    }
}

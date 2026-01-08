using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;

public partial class _Default : BasePage
{
    private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            EnsureDefaultFilter();
            LoadDashboard();
        }
    }

    protected void btnApplyFilter_Click(object sender, EventArgs e)
    {
        EnsureDefaultFilter();
        LoadDashboard();
    }

    protected void btnClearFilter_Click(object sender, EventArgs e)
    {
        txtFromDate.Text = string.Empty;
        txtToDate.Text = string.Empty;
        EnsureDefaultFilter();
        LoadDashboard();
    }

    private void LoadDashboard()
    {
        var range = GetDateRange();
        var summary = GetSummary();
        ltBookingsToday.Text = summary.BookingsToday.ToString("N0", VnCulture);
        ltBookingsWeek.Text = summary.BookingsWeek.ToString("N0", VnCulture);
        ltBookingsMonth.Text = summary.BookingsMonth.ToString("N0", VnCulture);
        ltRevenueToday.Text = summary.RevenueToday.ToString("N0", VnCulture);
        ltRevenueMonth.Text = summary.RevenueMonth.ToString("N0", VnCulture);
        ltTotalCustomers.Text = summary.TotalCustomers.ToString("N0", VnCulture);
        ltOverdueCount.Text = summary.OverdueCount.ToString("N0", VnCulture);
        ltNewCustomersMonth.Text = summary.NewCustomersMonth.ToString("N0", VnCulture);
        ltReturningCustomersMonth.Text = summary.ReturningCustomersMonth.ToString("N0", VnCulture);

        if (summary.BookingsMonth > 0)
        {
            var fpRate = (decimal)summary.FpCountMonth * 100m / summary.BookingsMonth;
            ltFpRateMonth.Text = fpRate.ToString("N1", VnCulture) + "%";
        }
        else
        {
            ltFpRateMonth.Text = "0%";
        }

        ltStatusOP.Text = summary.StatusOP.ToString("N0", VnCulture);
        ltStatusCX.Text = summary.StatusCX.ToString("N0", VnCulture);
        ltStatusBK.Text = summary.StatusBK.ToString("N0", VnCulture);
        ltStatusFP.Text = summary.StatusFP.ToString("N0", VnCulture);

        rptPendingToday.DataSource = GetPendingToday(range);
        rptPendingToday.DataBind();

        var sourceItems = GetTopSources(range);
        rptSources.DataSource = sourceItems;
        rptSources.DataBind();

        var creatorItems = GetTopCreators(range);
        rptCreators.DataSource = creatorItems;
        rptCreators.DataBind();

        rptOverdue.DataSource = GetOverdue(range);
        rptOverdue.DataBind();

        rptDeadlines.DataSource = GetUpcomingDeadlines(range);
        rptDeadlines.DataBind();

        var trendItems = GetTrend(range);
        rptTrend.DataSource = trendItems;
        rptTrend.DataBind();

        BuildChartData(summary, sourceItems, trendItems);
    }

    private DashboardSummary GetSummary()
    {
        var range = GetDateRange();
        var result = new DashboardSummary();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
SELECT
    SUM(CASE WHEN CONVERT(date, o.Creationdate) = @today
                  AND @today BETWEEN @from AND @to THEN 1 ELSE 0 END) AS BookingsToday,
    SUM(CASE WHEN CONVERT(date, o.Creationdate) = @today
                  AND @today BETWEEN @from AND @to THEN ISNULL(o.Amountthucban, 0) ELSE 0 END) AS RevenueToday,
    SUM(CASE WHEN o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to) THEN 1 ELSE 0 END) AS BookingsWeek,
    SUM(CASE WHEN o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to) THEN 1 ELSE 0 END) AS BookingsMonth,
    SUM(CASE WHEN o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to) THEN ISNULL(o.Amountthucban, 0) ELSE 0 END) AS RevenueMonth
FROM [order] o
WHERE o.Visible = 1;
";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.BookingsToday = SafeInt(reader["BookingsToday"]);
                        result.RevenueToday = SafeDecimal(reader["RevenueToday"]);
                        result.BookingsWeek = SafeInt(reader["BookingsWeek"]);
                        result.BookingsMonth = SafeInt(reader["BookingsMonth"]);
                        result.RevenueMonth = SafeDecimal(reader["RevenueMonth"]);
                    }
                }
            }

            var totalSql = @"
SELECT COUNT(DISTINCT LTRIM(RTRIM(ctTel)))
FROM [order]
WHERE Visible = 1
  AND LTRIM(RTRIM(ISNULL(ctTel, ''))) <> ''
  AND Creationdate >= @fromDate AND Creationdate < DATEADD(day, 1, @toDate);";
            using (var cmd = new SqlCommand(totalSql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                result.TotalCustomers = Convert.ToInt32(cmd.ExecuteScalar());
            }

            var statusSql = @"
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
SELECT
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) = 0 AND o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) >= @today THEN 1 ELSE 0 END) AS OP,
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) = 0 AND o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < @today THEN 1 ELSE 0 END) AS CX,
    SUM(CASE WHEN ISNULL(pay.TotalPaid, 0) > 0 AND ISNULL(o.Amountthucban, 0) > ISNULL(pay.TotalPaid, 0) THEN 1 ELSE 0 END) AS BK,
    SUM(CASE WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 1 ELSE 0 END) AS FP,
    SUM(CASE WHEN o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
             AND ISNULL(o.Amountthucban, 0) > 0
             AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0)
        THEN 1 ELSE 0 END) AS FPMonth
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1
  AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to);
";
            using (var cmd = new SqlCommand(statusSql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.StatusOP = SafeInt(reader["OP"]);
                        result.StatusCX = SafeInt(reader["CX"]);
                        result.StatusBK = SafeInt(reader["BK"]);
                        result.StatusFP = SafeInt(reader["FP"]);
                        result.FpCountMonth = SafeInt(reader["FPMonth"]);
                        result.OverdueCount = result.StatusCX;
                    }
                }
            }

            var customerSql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH phones_month AS (
    SELECT DISTINCT LTRIM(RTRIM(ctTel)) AS Phone
    FROM [order] o
    WHERE o.Visible = 1
      AND LTRIM(RTRIM(ISNULL(ctTel, ''))) <> ''
      AND o.Creationdate >= @from
      AND o.Creationdate < DATEADD(day, 1, @to)
),
first_order AS (
    SELECT LTRIM(RTRIM(ctTel)) AS Phone,
           MIN(o.Creationdate) AS FirstDate
    FROM [order] o
    WHERE o.Visible = 1
      AND LTRIM(RTRIM(ISNULL(ctTel, ''))) <> ''
    GROUP BY LTRIM(RTRIM(ctTel))
)
SELECT
    SUM(CASE WHEN f.FirstDate >= @from AND f.FirstDate < DATEADD(day, 1, @to) THEN 1 ELSE 0 END) AS NewCustomers,
    SUM(CASE WHEN f.FirstDate < @from AND m.Phone IS NOT NULL THEN 1 ELSE 0 END) AS ReturningCustomers
FROM first_order f
LEFT JOIN phones_month m ON m.Phone = f.Phone;";
            using (var cmd = new SqlCommand(customerSql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.NewCustomersMonth = SafeInt(reader["NewCustomers"]);
                        result.ReturningCustomersMonth = SafeInt(reader["ReturningCustomers"]);
                    }
                }
            }
        }
        return result;
    }

    private List<SourceStat> GetTopSources(DateRange range)
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
  AND Creationdate >= @fromDate AND Creationdate < DATEADD(day, 1, @toDate)
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(ctnguon)), ''), '(Chưa có)')
ORDER BY Total DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
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

    private List<CreatorStat> GetTopCreators(DateRange range)
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
  AND o.Creationdate >= @fromDate AND o.Creationdate < DATEADD(day, 1, @toDate)
GROUP BY ISNULL(NULLIF(u.username, ''), '(Không rõ)')
ORDER BY Total DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
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

    private List<DeadlineItem> GetUpcomingDeadlines(DateRange range)
    {
        var items = new List<DeadlineItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
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
  AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
  AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname, '')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname, '')))) <> 'tour leader'
  AND ISNULL(pay.TotalPaid, 0) = 0
ORDER BY o.DepositDeadline ASC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
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

    private List<PendingItem> GetPendingToday(DateRange range)
    {
        var items = new List<PendingItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
SELECT TOP 8
    o.orderid,
    o.ctLastname,
    o.ctFirstname,
    o.Creationdate
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1
  AND CONVERT(date, o.Creationdate) = @today
  AND @today BETWEEN @from AND @to
  AND ISNULL(pay.TotalPaid, 0) = 0
ORDER BY o.Creationdate DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PendingItem
                        {
                            OrderId = reader["orderid"].ToString(),
                            CustomerName = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string),
                            CreatedDate = reader["Creationdate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["Creationdate"]).ToString("dd/MM/yyyy")
                                : string.Empty
                        });
                    }
                }
            }
        }
        return items;
    }

    private List<DeadlineItem> GetOverdue(DateRange range)
    {
        var items = new List<DeadlineItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
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
  AND CONVERT(date, o.DepositDeadline) < @today
  AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
  AND LOWER(LTRIM(RTRIM(ISNULL(o.ctLastname, '')))) + ' ' + LOWER(LTRIM(RTRIM(ISNULL(o.ctFirstname, '')))) <> 'tour leader'
  AND ISNULL(pay.TotalPaid, 0) = 0
ORDER BY o.DepositDeadline ASC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
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

    private List<TrendItem> GetTrend(DateRange range)
    {
        var items = new List<TrendItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH data AS (
    SELECT
        CONVERT(date, o.Creationdate) AS DayKey,
        COUNT(*) AS Total,
        SUM(ISNULL(o.Amountthucban, 0)) AS Revenue
    FROM [order] o
    WHERE o.Visible = 1
      AND o.Creationdate >= @from
      AND o.Creationdate < DATEADD(day, 1, @to)
    GROUP BY CONVERT(date, o.Creationdate)
),
top_days AS (
    SELECT TOP 7 *
    FROM data
    ORDER BY DayKey DESC
)
SELECT DayKey, Total, Revenue
FROM top_days
ORDER BY DayKey ASC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
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

    private void BuildChartData(DashboardSummary summary, List<SourceStat> sourceItems, List<TrendItem> trendItems)
    {
        var sourceLabels = sourceItems.Select(x => x.Source ?? string.Empty).ToList();
        var sourceCounts = sourceItems.Select(x => x.Total).ToList();

        ltChartTrendLabels.Text = ToJsArray(trendItems.Select(x => x.Date));
        ltChartTrendRevenue.Text = ToJsArray(trendItems.Select(x => ParseDecimal(x.Revenue)));
        ltChartSourceLabels.Text = ToJsArray(sourceLabels);
        ltChartSourceCounts.Text = ToJsArray(sourceCounts);

        ltChartStatusLabels.Text = ToJsArray(new[] { "OP", "CX", "BK", "FP" });
        ltChartStatusCounts.Text = ToJsArray(new[] {
            summary.StatusOP,
            summary.StatusCX,
            summary.StatusBK,
            summary.StatusFP
        });
    }

    private string ToJsArray(IEnumerable<string> values)
    {
        var items = values.Select(v => "'" + HttpUtility.JavaScriptStringEncode(v ?? string.Empty) + "'");
        return "[" + string.Join(",", items) + "]";
    }

    private string ToJsArray(IEnumerable<int> values)
    {
        return "[" + string.Join(",", values) + "]";
    }

    private string ToJsArray(IEnumerable<decimal> values)
    {
        var items = values.Select(v => v.ToString(CultureInfo.InvariantCulture));
        return "[" + string.Join(",", items) + "]";
    }

    private decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        decimal parsed;
        if (decimal.TryParse(value, NumberStyles.Any, VnCulture, out parsed)) return parsed;
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) return parsed;
        return 0m;
    }

    private void EnsureDefaultFilter()
    {
        var range = GetDateRange();
        txtFromDate.Text = range.From.ToString("yyyy-MM-dd");
        txtToDate.Text = range.To.ToString("yyyy-MM-dd");
    }

    private DateRange GetDateRange()
    {
        var from = ParseDateInput(txtFromDate.Text);
        var to = ParseDateInput(txtToDate.Text);

        if (!from.HasValue || !to.HasValue)
        {
            var now = DateTime.Today;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return new DateRange(start, end);
        }

        if (to.Value < from.Value)
        {
            return new DateRange(to.Value, from.Value);
        }

        return new DateRange(from.Value, to.Value);
    }

    private DateTime? ParseDateInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        DateTime parsed;
        if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.Date;
        }
        return null;
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
        public decimal RevenueToday { get; set; }
        public decimal RevenueMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int StatusOP { get; set; }
        public int StatusCX { get; set; }
        public int StatusBK { get; set; }
        public int StatusFP { get; set; }
        public int FpCountMonth { get; set; }
        public int OverdueCount { get; set; }
        public int NewCustomersMonth { get; set; }
        public int ReturningCustomersMonth { get; set; }
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

    private class PendingItem
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CreatedDate { get; set; }
    }

    private class TrendItem
    {
        public string Date { get; set; }
        public int Total { get; set; }
        public string Revenue { get; set; }
    }

    private struct DateRange
    {
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }

        public DateRange(DateTime from, DateTime to)
            : this()
        {
            From = from.Date;
            To = to.Date;
        }
    }
}

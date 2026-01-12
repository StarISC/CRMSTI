using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

public partial class Reports_MonthlyReport : BasePage
{
    private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            EnsureDefaultFilter();
            LoadReport();
        }
    }

    protected void btnApplyFilter_Click(object sender, EventArgs e)
    {
        EnsureDefaultFilter();
        LoadReport();
    }

    protected void btnClearFilter_Click(object sender, EventArgs e)
    {
        txtFromDate.Text = string.Empty;
        txtToDate.Text = string.Empty;
        EnsureDefaultFilter();
        LoadReport();
    }

    private void LoadReport()
    {
        var range = GetDateRange();
        var summary = GetSummary(range);

        ltTotalBookings.Text = summary.TotalBookings.ToString("N0", VnCulture);
        ltSuccessBookings.Text = summary.SuccessBookings.ToString("N0", VnCulture);
        ltTotalRevenue.Text = summary.TotalRevenue.ToString("N0", VnCulture);
        ltRevenuePerBooking.Text = summary.RevenuePerBooking.ToString("N0", VnCulture);
        ltStatusOP.Text = summary.StatusOP.ToString("N0", VnCulture);
        ltStatusCX.Text = summary.StatusCX.ToString("N0", VnCulture);
        ltStatusBK.Text = summary.StatusBK.ToString("N0", VnCulture);
        ltStatusFP.Text = summary.StatusFP.ToString("N0", VnCulture);
        ltOverdueCount.Text = summary.StatusCX.ToString("N0", VnCulture);
        ltConversionRate.Text = summary.ConversionRate.ToString("N1", VnCulture) + "%";
        ltNewCustomers.Text = summary.NewCustomers.ToString("N0", VnCulture);
        ltReturningCustomers.Text = summary.ReturningCustomers.ToString("N0", VnCulture);

        rptTopSources.DataSource = GetTopSources(range);
        rptTopSources.DataBind();

        rptTopStaff.DataSource = GetTopStaff(range);
        rptTopStaff.DataBind();

        rptTopCountries.DataSource = GetTopCountries(range);
        rptTopCountries.DataBind();

        rptTopDepartures.DataSource = GetTopDepartures(range);
        rptTopDepartures.DataBind();

        rptTopProducts.DataSource = GetTopProducts(range);
        rptTopProducts.DataBind();

        rptOverdueList.DataSource = GetOverdue(range);
        rptOverdueList.DataBind();

        rptUpcomingList.DataSource = GetUpcomingDeadlines(range);
        rptUpcomingList.DataBind();
    }
    private SummaryResult GetSummary(DateRange range)
    {
        var result = new SummaryResult();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH base AS (
    SELECT
        o.orderid,
        o.Amountthucban,
        o.Creationdate
    FROM [order] o
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
),
status_calc AS (
    SELECT
        o.orderid,
        o.Amountthucban,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
)
SELECT
    (SELECT COUNT(*) FROM base) AS TotalBookings,
    SUM(CASE WHEN Status IN ('BK','FP') THEN 1 ELSE 0 END) AS SuccessBookings,
    SUM(CASE WHEN Status = 'OP' THEN 1 ELSE 0 END) AS StatusOP,
    SUM(CASE WHEN Status = 'CX' THEN 1 ELSE 0 END) AS StatusCX,
    SUM(CASE WHEN Status = 'BK' THEN 1 ELSE 0 END) AS StatusBK,
    SUM(CASE WHEN Status = 'FP' THEN 1 ELSE 0 END) AS StatusFP,
    SUM(CASE WHEN Status IN ('BK','FP') THEN ISNULL(Amountthucban, 0) ELSE 0 END) AS TotalRevenue
FROM status_calc;";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.TotalBookings = SafeInt(reader["TotalBookings"]);
                        result.SuccessBookings = SafeInt(reader["SuccessBookings"]);
                        result.StatusOP = SafeInt(reader["StatusOP"]);
                        result.StatusCX = SafeInt(reader["StatusCX"]);
                        result.StatusBK = SafeInt(reader["StatusBK"]);
                        result.StatusFP = SafeInt(reader["StatusFP"]);
                        result.TotalRevenue = SafeDecimal(reader["TotalRevenue"]);
                    }
                }
            }

            result.RevenuePerBooking = result.SuccessBookings > 0
                ? result.TotalRevenue / result.SuccessBookings
                : 0m;

            var totalStatus = result.StatusOP + result.StatusCX + result.StatusBK + result.StatusFP;
            result.ConversionRate = totalStatus > 0
                ? ((decimal)(result.StatusBK + result.StatusFP) * 100m / totalStatus)
                : 0m;

            var customerSql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH phones_range AS (
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
    SUM(CASE WHEN f.FirstDate < @from AND r.Phone IS NOT NULL THEN 1 ELSE 0 END) AS ReturningCustomers
FROM first_order f
LEFT JOIN phones_range r ON r.Phone = f.Phone;";
            using (var cmd = new SqlCommand(customerSql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.NewCustomers = SafeInt(reader["NewCustomers"]);
                        result.ReturningCustomers = SafeInt(reader["ReturningCustomers"]);
                    }
                }
            }
        }
        return result;
    }

    private List<SourceRow> GetTopSources(DateRange range)
    {
        var items = new List<SourceRow>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH status_calc AS (
    SELECT
        o.orderid,
        o.ctnguon,
        o.Amountthucban,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
)
SELECT TOP 5
    ISNULL(NULLIF(LTRIM(RTRIM(ctnguon)), ''), N'(Chưa có)') AS Source,
    COUNT(*) AS TotalBookings,
    SUM(ISNULL(Amountthucban, 0)) AS Revenue
FROM status_calc
WHERE Status IN ('BK','FP')
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(ctnguon)), ''), N'(Chưa có)')
ORDER BY Revenue DESC, TotalBookings DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new SourceRow
                        {
                            Source = reader["Source"] as string,
                            TotalBookings = SafeInt(reader["TotalBookings"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
                        });
                    }
                }
            }
        }
        return items;
    }
    private List<StaffRow> GetTopStaff(DateRange range)
    {
        var items = new List<StaffRow>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH status_calc AS (
    SELECT
        o.orderid,
        o.userid,
        o.Amountthucban,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
)
SELECT TOP 5
    ISNULL(NULLIF(u.name, ''), N'(Không rõ)') AS StaffName,
    COUNT(*) AS TotalBookings,
    SUM(ISNULL(cust.CountCustomers, 0)) AS TotalGuests,
    SUM(ISNULL(sc.Amountthucban, 0)) AS Revenue
FROM status_calc sc
INNER JOIN users u ON u.id = sc.userid
OUTER APPLY (
    SELECT COUNT(*) AS CountCustomers
    FROM customer c
    WHERE c.orderID = sc.orderid AND c.Visible = 1
) cust
WHERE sc.Status IN ('BK','FP')
  AND (u.Role = 'user' OR u.Role = 'DevAgent' OR u.username = @specialUser)
GROUP BY ISNULL(NULLIF(u.name, ''), N'(Không rõ)')
ORDER BY Revenue DESC, TotalBookings DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.Parameters.AddWithValue("@specialUser", "vy.lephuong@startravelvn.com");
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new StaffRow
                        {
                            StaffName = reader["StaffName"] as string,
                            TotalBookings = SafeInt(reader["TotalBookings"]),
                            TotalGuests = SafeInt(reader["TotalGuests"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
                        });
                    }
                }
            }
        }
        return items;
    }

    private List<CountryRow> GetTopCountries(DateRange range)
    {
        var items = new List<CountryRow>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH status_calc AS (
    SELECT
        o.orderid,
        o.Amountthucban,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
),
order_country AS (
    SELECT
        sc.orderid,
        sc.Amountthucban,
        ISNULL(NULLIF(LTRIM(RTRIM(d.name)), ''), N'(Không rõ)') AS CountryName
    FROM status_calc sc
    JOIN customer c ON c.orderID = sc.orderid AND c.Visible = 1
    JOIN [product-des] pd ON pd.productID = c.ProductID
    JOIN destination d ON d.id = pd.destinationID
    WHERE sc.Status IN ('BK','FP')
)
SELECT
    CountryName,
    COUNT(DISTINCT orderid) AS TotalBookings,
    SUM(ISNULL(Amountthucban, 0)) AS Revenue
FROM order_country
GROUP BY CountryName
ORDER BY Revenue DESC, TotalBookings DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new CountryRow
                        {
                            CountryName = reader["CountryName"] as string,
                            TotalBookings = SafeInt(reader["TotalBookings"]),
                            RevenueValue = SafeDecimal(reader["Revenue"])
                        });
                    }
                }
            }
        }
        return GroupByMarket(items);
    }

    private List<DepartureRow> GetTopDepartures(DateRange range)
    {
        var items = new List<DepartureRow>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH status_calc AS (
    SELECT
        o.orderid,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
),
departures AS (
    SELECT
        sc.orderid,
        MIN(p.ngaydi) AS DepartureDate
    FROM status_calc sc
    JOIN customer c ON c.orderID = sc.orderid AND c.Visible = 1
    LEFT JOIN price p ON p.id = c.IDPrice
    WHERE sc.Status IN ('BK','FP')
    GROUP BY sc.orderid
)
SELECT TOP 5
    DepartureDate,
    COUNT(*) AS TotalBookings
FROM departures
WHERE DepartureDate IS NOT NULL
GROUP BY DepartureDate
ORDER BY TotalBookings DESC, DepartureDate DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new DepartureRow
                        {
                            DepartureDate = reader["DepartureDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["DepartureDate"]).ToString("dd/MM/yyyy")
                                : string.Empty,
                            TotalBookings = SafeInt(reader["TotalBookings"])
                        });
                    }
                }
            }
        }
        return items;
    }
    private List<ProductRow> GetTopProducts(DateRange range)
    {
        var items = new List<ProductRow>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
WITH status_calc AS (
    SELECT
        o.orderid,
        o.Amountthucban,
        CASE
            WHEN ISNULL(pay.TotalPaid, 0) = 0 THEN
                CASE
                    WHEN o.DepositDeadline IS NOT NULL AND CONVERT(date, o.DepositDeadline) < CONVERT(date, GETDATE()) THEN 'CX'
                    ELSE 'OP'
                END
            WHEN ISNULL(o.Amountthucban, 0) > 0 AND ISNULL(pay.TotalPaid, 0) >= ISNULL(o.Amountthucban, 0) THEN 'FP'
            ELSE 'BK'
        END AS Status
    FROM [order] o
    OUTER APPLY (
        SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
        FROM payment p
        WHERE p.OrderId = o.orderid
    ) pay
    WHERE o.Visible = 1
      AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
),
product_data AS (
    SELECT
        sc.orderid,
        sc.Amountthucban,
        ISNULL(NULLIF(LTRIM(RTRIM(p.name)), ''), N'(Không rõ)') AS ProductName
    FROM status_calc sc
    JOIN customer c ON c.orderID = sc.orderid AND c.Visible = 1
    JOIN product p ON p.id = c.ProductID
    WHERE sc.Status IN ('BK','FP')
)
SELECT TOP 10
    ProductName,
    COUNT(DISTINCT orderid) AS TotalBookings,
    SUM(ISNULL(guest.CountGuests, 0)) AS TotalGuests,
    SUM(ISNULL(Amountthucban, 0)) AS Revenue
FROM product_data pd
OUTER APPLY (
    SELECT COUNT(*) AS CountGuests
    FROM customer c2
    WHERE c2.orderID = pd.orderid AND c2.Visible = 1
) guest
GROUP BY ProductName
ORDER BY Revenue DESC, TotalBookings DESC;";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", range.From);
                cmd.Parameters.AddWithValue("@toDate", range.To);
                cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ProductRow
                        {
                            ProductName = reader["ProductName"] as string,
                            TotalBookings = SafeInt(reader["TotalBookings"]),
                            TotalGuests = SafeInt(reader["TotalGuests"]),
                            Revenue = SafeDecimal(reader["Revenue"]).ToString("N0", VnCulture)
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
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
DECLARE @today date = CONVERT(date, GETDATE());
SELECT TOP 10
    o.orderid,
    o.ctLastname,
    o.ctFirstname,
    o.DepositDeadline
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1
  AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
  AND o.DepositDeadline IS NOT NULL
  AND CONVERT(date, o.DepositDeadline) < @today
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

    private List<DeadlineItem> GetUpcomingDeadlines(DateRange range)
    {
        var items = new List<DeadlineItem>();
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            var sql = @"
DECLARE @from date = @fromDate;
DECLARE @to date = @toDate;
DECLARE @today date = CONVERT(date, GETDATE());
SELECT TOP 10
    o.orderid,
    o.ctLastname,
    o.ctFirstname,
    o.DepositDeadline
FROM [order] o
OUTER APPLY (
    SELECT SUM(ISNULL(p.Amount, 0)) AS TotalPaid
    FROM payment p
    WHERE p.OrderId = o.orderid
) pay
WHERE o.Visible = 1
  AND o.Creationdate >= @from AND o.Creationdate < DATEADD(day, 1, @to)
  AND o.DepositDeadline IS NOT NULL
  AND CONVERT(date, o.DepositDeadline) BETWEEN @today AND DATEADD(day, 7, @today)
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

    private class SummaryResult
    {
        public int TotalBookings { get; set; }
        public int SuccessBookings { get; set; }
        public int StatusOP { get; set; }
        public int StatusCX { get; set; }
        public int StatusBK { get; set; }
        public int StatusFP { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenuePerBooking { get; set; }
        public decimal ConversionRate { get; set; }
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
    }

    private class SourceRow
    {
        public string Source { get; set; }
        public int TotalBookings { get; set; }
        public string Revenue { get; set; }
    }

    private class StaffRow
    {
        public string StaffName { get; set; }
        public int TotalBookings { get; set; }
        public int TotalGuests { get; set; }
        public string Revenue { get; set; }
    }

    private class CountryRow
    {
        public string Market { get; set; }
        public string CountryName { get; set; }
        public string CountriesHtml { get; set; }
        public int TotalBookings { get; set; }
        public string Revenue { get; set; }
        public decimal RevenueValue { get; set; }
    }

    private class DepartureRow
    {
        public string DepartureDate { get; set; }
        public int TotalBookings { get; set; }
    }

    private class ProductRow
    {
        public string ProductName { get; set; }
        public int TotalBookings { get; set; }
        public int TotalGuests { get; set; }
        public string Revenue { get; set; }
    }

    private class DeadlineItem
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string DepositDeadline { get; set; }
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

    private List<CountryRow> GroupByMarket(List<CountryRow> countryRows)
    {
        var marketMap = new Dictionary<string, CountryRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in countryRows)
        {
            var market = ResolveMarket(row.CountryName);
            CountryRow agg;
            if (!marketMap.TryGetValue(market, out agg))
            {
                agg = new CountryRow
                {
                    Market = market,
                    TotalBookings = 0,
                    RevenueValue = 0m,
                    CountriesHtml = string.Empty,
                    CountryName = string.Empty
                };
                marketMap[market] = agg;
            }

            agg.TotalBookings += row.TotalBookings;
            agg.RevenueValue += row.RevenueValue;

            var country = (row.CountryName ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(country))
            {
                if (agg.CountryName.Length == 0)
                {
                    agg.CountryName = "|" + country + "|";
                }
                else if (agg.CountryName.IndexOf("|" + country + "|", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    agg.CountryName += "|" + country + "|";
                }
            }
        }

        var results = new List<CountryRow>();
        foreach (var kv in marketMap)
        {
            var agg = kv.Value;
            var countries = new List<string>();
            if (!string.IsNullOrEmpty(agg.CountryName))
            {
                foreach (var part in agg.CountryName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var exists = false;
                    for (var i = 0; i < countries.Count; i++)
                    {
                        if (string.Equals(countries[i], part, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) countries.Add(part);
                }
            }
            agg.CountriesHtml = BuildCountryTags(countries);
            agg.Revenue = agg.RevenueValue.ToString("N0", VnCulture);
            results.Add(agg);
        }

        results.Sort((a, b) => b.RevenueValue.CompareTo(a.RevenueValue));
        return results;
    }

    private string BuildCountryTags(List<string> countries)
    {
        if (countries == null || countries.Count == 0) return string.Empty;
        countries.Sort(StringComparer.OrdinalIgnoreCase);
        var tags = new List<string>();
        foreach (var c in countries)
        {
            tags.Add("<span class=\"country-tag\">" + c + "</span>");
        }
        return string.Join("", tags);
    }

    private string ResolveMarket(string countryName)
    {
        var name = RemoveDiacritics((countryName ?? string.Empty).ToLowerInvariant());
        if (ContainsAny(name, new[] { "vietnam", "viet nam", "thai lan", "thailand", "singapore", "malaysia", "indonesia", "philippines", "philippine", "lao", "laos", "campuchia", "cambodia", "myanmar", "brunei", "timor" }))
            return "Đông Nam Á";
        if (ContainsAny(name, new[] { "nhat ban", "japan", "han quoc", "korea", "trung quoc", "china", "dai loan", "taiwan", "hong kong", "macau", "mong co", "mongolia" }))
            return "Đông Bắc Á";
        if (ContainsAny(name, new[] { "uae", "dubai", "abu dhabi", "qatar", "saudi", "oman", "kuwait", "bahrain", "jordan", "israel", "turkey", "iran" }))
            return "Trung Đông";
        if (ContainsAny(name, new[] { "phap", "france", "anh", "england", "uk", "germany", "duc", "italy", "y", "spain", "tay ban nha", "switzerland", "thuy si", "netherlands", "ha lan", "belgium", "ao", "austria", "czech", "hungary", "poland", "greece", "hy lap", "portugal", "norway", "sweden", "denmark", "finland", "russia" }))
            return "Châu Âu";
        if (ContainsAny(name, new[] { "hoa ky", "usa", "united states", "america", "alaska", "canada" }))
            return "Mỹ & Alaska (Canada)";
        if (ContainsAny(name, new[] { "australia", "new zealand", "newzealand", "nz" }))
            return "Úc & New Zealand";
        return "Khác";
    }

    private bool ContainsAny(string input, string[] keywords)
    {
        foreach (var k in keywords)
        {
            if (input.Contains(k)) return true;
        }
        return false;
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var chars = new List<char>();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                chars.Add(c);
            }
        }
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }
}

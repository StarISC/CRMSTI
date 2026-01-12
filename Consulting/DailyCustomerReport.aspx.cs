using System;
using System.Globalization;

public partial class Consulting_DailyCustomerReport : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            EnsureDefaultFilter();
        }
    }

    protected void btnApplyFilter_Click(object sender, EventArgs e)
    {
        EnsureDefaultFilter();
    }

    protected void btnClearFilter_Click(object sender, EventArgs e)
    {
        txtFromDate.Text = string.Empty;
        txtToDate.Text = string.Empty;
        EnsureDefaultFilter();
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

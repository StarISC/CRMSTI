using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;

public partial class Consulting_DailyCustomerReportApi : System.Web.UI.Page
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

        var init = (Request["init"] ?? string.Empty).Trim();
        var drawStr = Request["draw"] ?? "1";
        int draw = ParseInt(drawStr, 1);
        int start = ParseInt(Request["start"], 0);
        int length = ParseInt(Request["length"], 50);
        if (length <= 0) length = 50;
        if (length > 500) length = 500;

        var fromDate = ParseDate(Request["fromDate"]);
        var toDate = ParseDate(Request["toDate"]);
        string keyword = (Request["keyword"] ?? string.Empty).Trim();

        try
        {
            var table = GetReportData(fromDate, toDate);
            var columns = new List<string>();
            foreach (DataColumn col in table.Columns)
            {
                columns.Add(col.ColumnName);
            }

            if (init == "1")
            {
                WriteJson(new { columns = columns });
                return;
            }

            var total = table.Rows.Count;
            var filteredRows = FilterRows(table, keyword);
            var filtered = filteredRows.Count;

            var pageRows = new List<Dictionary<string, object>>();
            var end = Math.Min(start + length, filteredRows.Count);
            for (int i = start; i < end; i++)
            {
                var row = filteredRows[i];
                var dataRow = new Dictionary<string, object>();
                foreach (var col in columns)
                {
                    dataRow[col] = row[col];
                }
                pageRows.Add(dataRow);
            }

            WriteJson(new
            {
                draw = draw,
                recordsTotal = total,
                recordsFiltered = filtered,
                data = pageRows
            });
        }
        catch (Exception ex)
        {
            WriteJson(new { error = ex.Message, data = new object[0], recordsTotal = 0, recordsFiltered = 0 });
        }
    }

    private DataTable GetReportData(DateTime? fromDate, DateTime? toDate)
    {
        using (var conn = Db.CreateConnection())
        {
            conn.Open();
            try
            {
                return ExecuteReport(conn, fromDate, toDate, true);
            }
            catch (SqlException ex)
            {
                if (ex.Message.IndexOf("too many arguments", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ExecuteReport(conn, null, null, false);
                }
                throw;
            }
        }
    }

    private DataTable ExecuteReport(SqlConnection conn, DateTime? fromDate, DateTime? toDate, bool includeParams)
    {
        using (var cmd = new SqlCommand("proGetReportCustomer", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = Db.CommandTimeoutSeconds;
            if (includeParams)
            {
                cmd.Parameters.AddWithValue("@fromDate", (object)(fromDate ?? (object)DBNull.Value));
                cmd.Parameters.AddWithValue("@toDate", (object)(toDate ?? (object)DBNull.Value));
            }
            using (var adapter = new SqlDataAdapter(cmd))
            {
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }
    }

    private List<DataRow> FilterRows(DataTable table, string keyword)
    {
        var rows = new List<DataRow>(table.Rows.Count);
        if (table.Rows.Count == 0) return rows;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            foreach (DataRow row in table.Rows) rows.Add(row);
            return rows;
        }

        var key = keyword.ToLowerInvariant();
        foreach (DataRow row in table.Rows)
        {
            foreach (DataColumn col in table.Columns)
            {
                var value = row[col];
                if (value == null || value == DBNull.Value) continue;
                if (value.ToString().ToLowerInvariant().Contains(key))
                {
                    rows.Add(row);
                    break;
                }
            }
        }
        return rows;
    }

    private DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        DateTime parsed;
        if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.Date;
        }
        return null;
    }

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
    }

    private void WriteJson(object payload)
    {
        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;
        Response.Write(serializer.Serialize(payload));
        HttpContext.Current.ApplicationInstance.CompleteRequest();
    }
}

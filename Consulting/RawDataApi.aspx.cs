using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class Consulting_RawDataApi : System.Web.UI.Page
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

        var drawStr = Request["draw"] ?? "1";
        int draw = ParseInt(drawStr, 1);
        int start = ParseInt(Request["start"], 0);
        int length = ParseInt(Request["length"], 50);
        if (length <= 0) length = 50;
        if (length > 500) length = 500;

        string keyword = (Request["keyword"] ?? string.Empty).Trim();

        var where = "WHERE 1=1";
        var parameters = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where += @" AND (
                company_name LIKE @kw OR tax_code LIKE @kw OR representative LIKE @kw
                OR phone LIKE @kw OR address LIKE @kw OR province LIKE @kw OR province_from_address LIKE @kw
            )";
            parameters.Add(new SqlParameter("@kw", "%" + keyword + "%"));
        }

        try
        {
            long total = 0;
            long filtered = 0;
            var data = new List<Dictionary<string, object>>();

            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.cf_raw_contacts", conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                using (var countFilteredCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.cf_raw_contacts " + where, conn))
                {
                    countFilteredCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countFilteredCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    filtered = Convert.ToInt64(countFilteredCmd.ExecuteScalar());
                }

                var sql = @"
SELECT company_name, tax_code, representative, phone, address, province_from_address, detail_url
FROM dbo.cf_raw_contacts
" + where + @"
ORDER BY created_at DESC, id DESC
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
                            row["CompanyName"] = reader["company_name"];
                            row["TaxCode"] = reader["tax_code"];
                            row["Representative"] = reader["representative"];
                            row["Phone"] = reader["phone"];
                            row["Address"] = reader["address"];
                            row["ProvinceFromAddress"] = reader["province_from_address"];
                            row["DetailUrl"] = reader["detail_url"];
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
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            Response.Write(serializer.Serialize(result));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
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

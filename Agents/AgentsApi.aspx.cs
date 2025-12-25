using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentsApi : System.Web.UI.Page
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

        string name = (Request["name"] ?? string.Empty).Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string type = (Request["type"] ?? string.Empty).Trim();
        string status = (Request["status"] ?? string.Empty).Trim();

        var parameters = new List<SqlParameter>();
        var where = "WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(name))
        {
            where += " AND a.name LIKE @name";
            parameters.Add(new SqlParameter("@name", "%" + name + "%"));
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            where += " AND a.phone LIKE @phone";
            parameters.Add(new SqlParameter("@phone", "%" + phone + "%"));
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            where += " AND a.agent_type = @type";
            parameters.Add(new SqlParameter("@type", type));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            where += " AND a.status = @status";
            parameters.Add(new SqlParameter("@status", status));
        }

        long total = 0;
        long filtered = 0;
        var data = new List<Dictionary<string, object>>();

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.cf_agents", conn))
                {
                    countCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    total = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                using (var countFilteredCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.cf_agents a " + where, conn))
                {
                    countFilteredCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    foreach (var p in parameters) countFilteredCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    filtered = Convert.ToInt64(countFilteredCmd.ExecuteScalar());
                }

                var sql = @"
SELECT a.id, a.code, a.name, a.phone, a.email, a.agent_type, a.status,
       a.representative_name, a.commission_rate,
       p.name AS ParentName
FROM dbo.cf_agents a
LEFT JOIN dbo.cf_agents p ON p.id = a.parent_agent_id
" + where + @"
ORDER BY a.created_at DESC, a.id DESC
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
                            row["Id"] = reader["id"];
                            row["Code"] = reader["code"];
                            row["Name"] = reader["name"];
                            row["Phone"] = reader["phone"];
                            row["Email"] = reader["email"];
                            row["AgentType"] = reader["agent_type"];
                            row["Status"] = reader["status"];
                            row["ParentName"] = reader["ParentName"] as string;
                            row["RepresentativeName"] = reader["representative_name"] as string;
                            row["CommissionRate"] = reader["commission_rate"];
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

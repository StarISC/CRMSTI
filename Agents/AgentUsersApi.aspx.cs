using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentUsersApi : System.Web.UI.Page
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

        int agentId = ParseInt(Request["id"], 0);
        if (agentId <= 0)
        {
            Response.Write(serializer.Serialize(new { data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        var data = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = @"
SELECT id, username, full_name, phone, email, is_active
FROM dbo.cf_agent_users
WHERE agent_id = @agentId
ORDER BY id DESC;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@agentId", agentId));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["Id"] = reader["id"];
                            row["Username"] = reader["username"];
                            row["FullName"] = reader["full_name"] as string;
                            row["Phone"] = reader["phone"] as string;
                            row["Email"] = reader["email"] as string;
                            row["IsActive"] = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"]);
                            data.Add(row);
                        }
                    }
                }
            }

            Response.Write(serializer.Serialize(new { data = data }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.StatusCode = 200;
            Response.Write(serializer.Serialize(new { error = ex.Message, data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
    }
}

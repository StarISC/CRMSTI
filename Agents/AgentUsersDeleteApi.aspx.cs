using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentUsersDeleteApi : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            Response.StatusCode = 401;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        var role = Session["Role"] as string;
        if (string.IsNullOrWhiteSpace(role) || !role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = 403;
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

        int id = ParseInt(Request.Form["id"], 0);
        int agentId = ParseInt(Request.Form["agentId"], 0);

        if (id <= 0 || agentId <= 0)
        {
            Response.Write(serializer.Serialize(new { error = "Thiếu thông tin cần thiết." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = "DELETE FROM dbo.cf_agent_users WHERE id = @id AND agent_id = @agent_id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@agent_id", agentId);
                    cmd.ExecuteNonQuery();
                }
            }

            Response.Write(serializer.Serialize(new { ok = true }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.StatusCode = 200;
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
    }
}

using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentUsersSaveApi : System.Web.UI.Page
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
        string username = (Request.Form["username"] ?? string.Empty).Trim();
        string password = (Request.Form["password"] ?? string.Empty).Trim();
        string fullName = (Request.Form["fullName"] ?? string.Empty).Trim();
        string phone = (Request.Form["phone"] ?? string.Empty).Trim();
        string email = (Request.Form["email"] ?? string.Empty).Trim();
        bool isActive = (Request.Form["isActive"] ?? "0") == "1";

        if (agentId <= 0)
        {
            Response.Write(serializer.Serialize(new { error = "Thiếu thông tin đại lý." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            Response.Write(serializer.Serialize(new { error = "Username không được để trống." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        if (id <= 0 && string.IsNullOrWhiteSpace(password))
        {
            Response.Write(serializer.Serialize(new { error = "Mật khẩu không được để trống." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();

                if (id > 0)
                {
                    var sql = @"
UPDATE dbo.cf_agent_users
SET username = @username,
    full_name = @full_name,
    phone = @phone,
    email = @email,
    is_active = @is_active
WHERE id = @id AND agent_id = @agent_id;";

                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        sql = @"
UPDATE dbo.cf_agent_users
SET username = @username,
    password = @password,
    full_name = @full_name,
    phone = @phone,
    email = @email,
    is_active = @is_active
WHERE id = @id AND agent_id = @agent_id;";
                    }

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@full_name", string.IsNullOrWhiteSpace(fullName) ? (object)DBNull.Value : fullName);
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                        cmd.Parameters.AddWithValue("@is_active", isActive);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@agent_id", agentId);
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            cmd.Parameters.AddWithValue("@password", password);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var sql = @"
INSERT INTO dbo.cf_agent_users (agent_id, username, password, full_name, phone, email, is_active)
VALUES (@agent_id, @username, @password, @full_name, @phone, @email, @is_active);";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                        cmd.Parameters.AddWithValue("@agent_id", agentId);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@full_name", string.IsNullOrWhiteSpace(fullName) ? (object)DBNull.Value : fullName);
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                        cmd.Parameters.AddWithValue("@is_active", isActive);
                        cmd.ExecuteNonQuery();
                    }
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

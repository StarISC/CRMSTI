using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentsUploadApi : System.Web.UI.Page
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
        bool isAdmin = !string.IsNullOrEmpty(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin)
        {
            Response.StatusCode = 403;
            Response.Write(new JavaScriptSerializer().Serialize(new { error = "Forbidden" }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        Response.ContentType = "application/json";
        Response.TrySkipIisCustomErrors = true;
        Response.Clear();
        Response.Buffer = true;
        Response.Charset = "utf-8";

        int agentId;
        if (!int.TryParse(Request["agentId"], out agentId))
        {
            Response.Write(new JavaScriptSerializer().Serialize(new { error = "AgentId không hợp lệ" }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        string docType = (Request["docType"] ?? "OTHER").Trim();
        string docNo = (Request["docNo"] ?? string.Empty).Trim();
        string docDateStr = (Request["docDate"] ?? string.Empty).Trim();
        DateTime? docDate = ParseDate(docDateStr);

        try
        {
            var baseDir = Server.MapPath("~/Uploads/Agents");
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var saved = new List<object>();
            foreach (string key in Request.Files)
            {
                var file = Request.Files[key];
                if (file == null || file.ContentLength <= 0) continue;

                var fileName = Path.GetFileName(file.FileName);
                var safeName = Guid.NewGuid().ToString("N") + "_" + fileName;
                var relativePath = "~/Uploads/Agents/" + safeName;
                var physicalPath = Path.Combine(baseDir, safeName);
                file.SaveAs(physicalPath);

                using (var conn = Db.CreateConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO dbo.cf_agent_documents
                        (agent_id, doc_type, doc_no, doc_date, file_name, file_path)
                        VALUES (@agentId, @docType, @docNo, @docDate, @fileName, @filePath)";
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@agentId", agentId));
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@docType", string.IsNullOrWhiteSpace(docType) ? "OTHER" : docType));
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@docNo", (object)docNo ?? DBNull.Value));
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@docDate", (object)docDate ?? DBNull.Value));
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@fileName", fileName));
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@filePath", relativePath));
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                saved.Add(new { fileName = fileName, filePath = relativePath });
            }

            Response.Write(new JavaScriptSerializer().Serialize(new { success = true, files = saved }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Write(new JavaScriptSerializer().Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private DateTime? ParseDate(string input)
    {
        DateTime val;
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (DateTime.TryParse(input, out val)) return val;
        return null;
    }
}

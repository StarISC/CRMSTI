using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentDocumentsApi : System.Web.UI.Page
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

        int id;
        if (!int.TryParse(Request["id"], out id))
        {
            Response.Write(new JavaScriptSerializer().Serialize(new { error = "Id không hợp lệ", data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;
        var data = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = @"
SELECT doc_type, doc_no, doc_date, file_name, file_path
FROM dbo.cf_agent_documents
WHERE agent_id = @id
ORDER BY created_at DESC;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["DocType"] = reader["doc_type"] as string;
                            row["DocNo"] = reader["doc_no"] as string;
                            row["DocDate"] = reader["doc_date"] != DBNull.Value ? Convert.ToDateTime(reader["doc_date"]).ToString("dd/MM/yyyy") : "";
                            row["FileName"] = reader["file_name"] as string;
                            row["FilePath"] = reader["file_path"] as string;
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
            Response.Write(serializer.Serialize(new { error = ex.Message, data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}

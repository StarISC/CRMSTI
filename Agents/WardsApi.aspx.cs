using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class WardsApi : System.Web.UI.Page
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
        var data = new List<Dictionary<string, object>>();

        int provinceId = ParseInt(Request["provinceId"], 0);

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = @"
SELECT id, name
FROM dbo.cf_wards
WHERE is_active = 1 AND (@provinceId = 0 OR province_id = @provinceId)
ORDER BY name;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.AddWithValue("@provinceId", provinceId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["Id"] = reader["id"];
                            row["Name"] = reader["name"] as string;
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

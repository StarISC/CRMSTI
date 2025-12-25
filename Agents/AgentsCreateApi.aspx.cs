using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentsCreateApi : System.Web.UI.Page
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

        string name = (Request["name"] ?? string.Empty).Trim();
        string type = (Request["type"] ?? "COMPANY").Trim();
        string status = (Request["status"] ?? "ACTIVE").Trim();
        string phone = (Request["phone"] ?? string.Empty).Trim();
        string email = (Request["email"] ?? string.Empty).Trim();
        string taxCode = (Request["taxCode"] ?? string.Empty).Trim();
        string taxAddress = (Request["taxAddress"] ?? string.Empty).Trim();
        string parentId = (Request["parentId"] ?? string.Empty).Trim();
        string commissionRate = (Request["commissionRate"] ?? string.Empty).Trim();
        string repName = (Request["representativeName"] ?? string.Empty).Trim();
        string repPhone = (Request["representativePhone"] ?? string.Empty).Trim();
        string contractNo = (Request["contractNo"] ?? string.Empty).Trim();
        string contractDate = (Request["contractDate"] ?? string.Empty).Trim();
        string contractExpiry = (Request["contractExpiry"] ?? string.Empty).Trim();
        string licenseNo = (Request["licenseNo"] ?? string.Empty).Trim();
        string licenseDate = (Request["licenseDate"] ?? string.Empty).Trim();
        string licenseExpiry = (Request["licenseExpiry"] ?? string.Empty).Trim();
        string note = (Request["note"] ?? string.Empty).Trim();
        string provinceId = (Request["provinceId"] ?? string.Empty).Trim();
        string wardId = (Request["wardId"] ?? string.Empty).Trim();
        string houseNo = (Request["houseNo"] ?? string.Empty).Trim();
        string street = (Request["street"] ?? string.Empty).Trim();
        string fullAddress = (Request["fullAddress"] ?? string.Empty).Trim();

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        if (string.IsNullOrWhiteSpace(name))
        {
            Response.Write(serializer.Serialize(new { error = "Tên đại lý không được để trống" }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        decimal commission = 0m;
        if (!string.IsNullOrWhiteSpace(commissionRate))
        {
            decimal.TryParse(commissionRate, out commission);
        }

        int? parent = null;
        int parsedParent;
        if (!string.IsNullOrWhiteSpace(parentId) && int.TryParse(parentId, out parsedParent))
        {
            parent = parsedParent;
        }

        DateTime? contractDateVal = ParseDate(contractDate);
        DateTime? contractExpiryVal = ParseDate(contractExpiry);
        DateTime? licenseDateVal = ParseDate(licenseDate);
        DateTime? licenseExpiryVal = ParseDate(licenseExpiry);
        int? province = ParseNullableInt(provinceId);
        int? ward = ParseNullableInt(wardId);

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = @"
INSERT INTO dbo.cf_agents
    (code, name, phone, email, tax_code, tax_address, status, note, agent_type, parent_agent_id, representative_name, representative_phone, commission_rate,
     contract_no, contract_date, contract_expiry, license_no, license_date, license_expiry)
VALUES
    (@code, @name, @phone, @email, @taxCode, @taxAddress, @status, @note, @type, @parentId, @repName, @repPhone, @commissionRate,
     @contractNo, @contractDate, @contractExpiry, @licenseNo, @licenseDate, @licenseExpiry);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@code", DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@name", name));
                    cmd.Parameters.Add(new SqlParameter("@phone", (object)phone ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@email", (object)email ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@taxCode", (object)taxCode ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@taxAddress", (object)taxAddress ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@status", (object)status ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@note", (object)note ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@type", type));
                    cmd.Parameters.Add(new SqlParameter("@parentId", (object)parent ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@repName", (object)repName ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@repPhone", (object)repPhone ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@commissionRate", commission));
                    cmd.Parameters.Add(new SqlParameter("@contractNo", (object)contractNo ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@contractDate", (object)contractDateVal ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@contractExpiry", (object)contractExpiryVal ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@licenseNo", (object)licenseNo ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@licenseDate", (object)licenseDateVal ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@licenseExpiry", (object)licenseExpiryVal ?? DBNull.Value));
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());

                    if ((province.HasValue && ward.HasValue) || !string.IsNullOrWhiteSpace(houseNo) || !string.IsNullOrWhiteSpace(street) || !string.IsNullOrWhiteSpace(fullAddress))
                    {
                        var addressSql = @"
INSERT INTO dbo.cf_agent_addresses (agent_id, house_no, street, ward_id, province_id, full_address)
VALUES (@agent_id, @house_no, @street, @ward_id, @province_id, @full_address);";
                        using (var addressCmd = new SqlCommand(addressSql, conn))
                        {
                            addressCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                            addressCmd.Parameters.AddWithValue("@agent_id", newId);
                            addressCmd.Parameters.AddWithValue("@house_no", string.IsNullOrWhiteSpace(houseNo) ? (object)DBNull.Value : houseNo);
                            addressCmd.Parameters.AddWithValue("@street", string.IsNullOrWhiteSpace(street) ? (object)DBNull.Value : street);
                            addressCmd.Parameters.AddWithValue("@ward_id", (object)ward ?? DBNull.Value);
                            addressCmd.Parameters.AddWithValue("@province_id", (object)province ?? DBNull.Value);
                            addressCmd.Parameters.AddWithValue("@full_address", string.IsNullOrWhiteSpace(fullAddress) ? (object)DBNull.Value : fullAddress);
                            addressCmd.ExecuteNonQuery();
                        }
                    }

                    Response.Write(serializer.Serialize(new { success = true, agentId = newId }));
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write(serializer.Serialize(new { error = ex.Message }));
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

    private int? ParseNullableInt(string input)
    {
        int val;
        if (int.TryParse(input, out val) && val > 0) return val;
        return null;
    }
}

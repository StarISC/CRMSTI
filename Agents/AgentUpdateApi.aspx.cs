using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentUpdateApi : System.Web.UI.Page
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
        if (id <= 0)
        {
            Response.Write(serializer.Serialize(new { error = "Thiếu thông tin đại lý." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        string name = (Request.Form["name"] ?? string.Empty).Trim();
        string type = (Request.Form["type"] ?? "COMPANY").Trim();
        string status = (Request.Form["status"] ?? "ACTIVE").Trim();
        string phone = (Request.Form["phone"] ?? string.Empty).Trim();
        string email = (Request.Form["email"] ?? string.Empty).Trim();
        string taxCode = (Request.Form["taxCode"] ?? string.Empty).Trim();
        string taxAddress = (Request.Form["taxAddress"] ?? string.Empty).Trim();
        string commissionRate = (Request.Form["commissionRate"] ?? string.Empty).Trim();
        string repName = (Request.Form["representativeName"] ?? string.Empty).Trim();
        string repPhone = (Request.Form["representativePhone"] ?? string.Empty).Trim();
        string contractNo = (Request.Form["contractNo"] ?? string.Empty).Trim();
        string contractDate = (Request.Form["contractDate"] ?? string.Empty).Trim();
        string contractExpiry = (Request.Form["contractExpiry"] ?? string.Empty).Trim();
        string licenseNo = (Request.Form["licenseNo"] ?? string.Empty).Trim();
        string licenseDate = (Request.Form["licenseDate"] ?? string.Empty).Trim();
        string licenseExpiry = (Request.Form["licenseExpiry"] ?? string.Empty).Trim();
        string note = (Request.Form["note"] ?? string.Empty).Trim();

        string provinceId = (Request.Form["provinceId"] ?? string.Empty).Trim();
        string wardId = (Request.Form["wardId"] ?? string.Empty).Trim();
        string houseNo = (Request.Form["houseNo"] ?? string.Empty).Trim();
        string street = (Request.Form["street"] ?? string.Empty).Trim();
        string fullAddress = (Request.Form["fullAddress"] ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Response.Write(serializer.Serialize(new { error = "Tên đại lý không được để trống." }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        decimal commission = 0m;
        if (!string.IsNullOrWhiteSpace(commissionRate))
        {
            decimal.TryParse(commissionRate, out commission);
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
UPDATE dbo.cf_agents
SET name = @name,
    phone = @phone,
    email = @email,
    tax_code = @taxCode,
    tax_address = @taxAddress,
    status = @status,
    note = @note,
    agent_type = @type,
    representative_name = @repName,
    representative_phone = @repPhone,
    commission_rate = @commissionRate,
    contract_no = @contractNo,
    contract_date = @contractDate,
    contract_expiry = @contractExpiry,
    license_no = @licenseNo,
    license_date = @licenseDate,
    license_expiry = @licenseExpiry
WHERE id = @id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                    cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                    cmd.Parameters.AddWithValue("@taxCode", string.IsNullOrWhiteSpace(taxCode) ? (object)DBNull.Value : taxCode);
                    cmd.Parameters.AddWithValue("@taxAddress", string.IsNullOrWhiteSpace(taxAddress) ? (object)DBNull.Value : taxAddress);
                    cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? (object)DBNull.Value : status);
                    cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@repName", string.IsNullOrWhiteSpace(repName) ? (object)DBNull.Value : repName);
                    cmd.Parameters.AddWithValue("@repPhone", string.IsNullOrWhiteSpace(repPhone) ? (object)DBNull.Value : repPhone);
                    cmd.Parameters.AddWithValue("@commissionRate", commission);
                    cmd.Parameters.AddWithValue("@contractNo", string.IsNullOrWhiteSpace(contractNo) ? (object)DBNull.Value : contractNo);
                    cmd.Parameters.AddWithValue("@contractDate", (object)contractDateVal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@contractExpiry", (object)contractExpiryVal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@licenseNo", string.IsNullOrWhiteSpace(licenseNo) ? (object)DBNull.Value : licenseNo);
                    cmd.Parameters.AddWithValue("@licenseDate", (object)licenseDateVal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@licenseExpiry", (object)licenseExpiryVal ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                if ((province.HasValue && ward.HasValue) || !string.IsNullOrWhiteSpace(houseNo) || !string.IsNullOrWhiteSpace(street) || !string.IsNullOrWhiteSpace(fullAddress))
                {
                    int addressId = 0;
                    using (var findCmd = new SqlCommand("SELECT TOP 1 id FROM dbo.cf_agent_addresses WHERE agent_id = @agent_id ORDER BY created_at DESC, id DESC", conn))
                    {
                        findCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                        findCmd.Parameters.AddWithValue("@agent_id", id);
                        var obj = findCmd.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value) addressId = Convert.ToInt32(obj);
                    }

                    if (addressId > 0)
                    {
                        var updateAddr = @"
UPDATE dbo.cf_agent_addresses
SET house_no = @house_no,
    street = @street,
    ward_id = @ward_id,
    province_id = @province_id,
    full_address = @full_address
WHERE id = @id;";
                        using (var addrCmd = new SqlCommand(updateAddr, conn))
                        {
                            addrCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                            addrCmd.Parameters.AddWithValue("@house_no", string.IsNullOrWhiteSpace(houseNo) ? (object)DBNull.Value : houseNo);
                            addrCmd.Parameters.AddWithValue("@street", string.IsNullOrWhiteSpace(street) ? (object)DBNull.Value : street);
                            addrCmd.Parameters.AddWithValue("@ward_id", (object)ward ?? DBNull.Value);
                            addrCmd.Parameters.AddWithValue("@province_id", (object)province ?? DBNull.Value);
                            addrCmd.Parameters.AddWithValue("@full_address", string.IsNullOrWhiteSpace(fullAddress) ? (object)DBNull.Value : fullAddress);
                            addrCmd.Parameters.AddWithValue("@id", addressId);
                            addrCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var insertAddr = @"
INSERT INTO dbo.cf_agent_addresses (agent_id, house_no, street, ward_id, province_id, full_address)
VALUES (@agent_id, @house_no, @street, @ward_id, @province_id, @full_address);";
                        using (var addrCmd = new SqlCommand(insertAddr, conn))
                        {
                            addrCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                            addrCmd.Parameters.AddWithValue("@agent_id", id);
                            addrCmd.Parameters.AddWithValue("@house_no", string.IsNullOrWhiteSpace(houseNo) ? (object)DBNull.Value : houseNo);
                            addrCmd.Parameters.AddWithValue("@street", string.IsNullOrWhiteSpace(street) ? (object)DBNull.Value : street);
                            addrCmd.Parameters.AddWithValue("@ward_id", (object)ward ?? DBNull.Value);
                            addrCmd.Parameters.AddWithValue("@province_id", (object)province ?? DBNull.Value);
                            addrCmd.Parameters.AddWithValue("@full_address", string.IsNullOrWhiteSpace(fullAddress) ? (object)DBNull.Value : fullAddress);
                            addrCmd.ExecuteNonQuery();
                        }
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

    private int ParseInt(string input, int fallback)
    {
        int val;
        return int.TryParse(input, out val) ? val : fallback;
    }
}

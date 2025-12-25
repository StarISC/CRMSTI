using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class AgentDetailApi : System.Web.UI.Page
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
            Response.Write(new JavaScriptSerializer().Serialize(new { error = "Id không hợp lệ" }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        try
        {
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                var sql = @"
SELECT a.id, a.code, a.name, a.phone, a.email, a.tax_code, a.tax_address,
       a.status, a.agent_type, a.parent_agent_id, a.representative_name, a.representative_phone,
       a.commission_rate, a.note,
       a.contract_no, a.contract_date, a.contract_expiry,
       a.license_no, a.license_date, a.license_expiry,
       p.name AS ParentName,
       addr.id AS AddressId, addr.house_no, addr.street, addr.ward_id, addr.province_id, addr.full_address,
       w.name AS WardName, pr.name AS ProvinceName
FROM dbo.cf_agents a
LEFT JOIN dbo.cf_agents p ON p.id = a.parent_agent_id
OUTER APPLY (
    SELECT TOP 1 aa.id, aa.house_no, aa.street, aa.ward_id, aa.province_id, aa.full_address
    FROM dbo.cf_agent_addresses aa
    WHERE aa.agent_id = a.id
    ORDER BY aa.created_at DESC, aa.id DESC
) addr
LEFT JOIN dbo.cf_wards w ON w.id = addr.ward_id
LEFT JOIN dbo.cf_provinces pr ON pr.id = addr.province_id
WHERE a.id = @id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var obj = new Dictionary<string, object>();
                            obj["Id"] = reader["id"];
                            obj["Code"] = reader["code"] as string;
                            obj["Name"] = reader["name"] as string;
                            obj["Phone"] = reader["phone"] as string;
                            obj["Email"] = reader["email"] as string;
                            obj["TaxCode"] = reader["tax_code"] as string;
                            obj["TaxAddress"] = reader["tax_address"] as string;
                            obj["Status"] = reader["status"] as string;
                            obj["AgentType"] = reader["agent_type"] as string;
                            obj["ParentName"] = reader["ParentName"] as string;
                            obj["RepresentativeName"] = reader["representative_name"] as string;
                            obj["RepresentativePhone"] = reader["representative_phone"] as string;
                            obj["CommissionRate"] = reader["commission_rate"] != DBNull.Value ? Convert.ToDecimal(reader["commission_rate"]).ToString("N2") + "%" : "";
                            obj["Note"] = reader["note"] as string;
                            obj["ContractNo"] = reader["contract_no"] as string;
                            obj["ContractDate"] = reader["contract_date"] != DBNull.Value ? Convert.ToDateTime(reader["contract_date"]).ToString("dd/MM/yyyy") : "";
                            obj["ContractExpiry"] = reader["contract_expiry"] != DBNull.Value ? Convert.ToDateTime(reader["contract_expiry"]).ToString("dd/MM/yyyy") : "";
                            obj["LicenseNo"] = reader["license_no"] as string;
                            obj["LicenseDate"] = reader["license_date"] != DBNull.Value ? Convert.ToDateTime(reader["license_date"]).ToString("dd/MM/yyyy") : "";
                            obj["LicenseExpiry"] = reader["license_expiry"] != DBNull.Value ? Convert.ToDateTime(reader["license_expiry"]).ToString("dd/MM/yyyy") : "";
                            obj["AddressId"] = reader["AddressId"] != DBNull.Value ? Convert.ToInt32(reader["AddressId"]) : 0;
                            obj["HouseNo"] = reader["house_no"] as string;
                            obj["Street"] = reader["street"] as string;
                            obj["WardId"] = reader["ward_id"] != DBNull.Value ? Convert.ToInt32(reader["ward_id"]) : 0;
                            obj["ProvinceId"] = reader["province_id"] != DBNull.Value ? Convert.ToInt32(reader["province_id"]) : 0;
                            obj["WardName"] = reader["WardName"] as string;
                            obj["ProvinceName"] = reader["ProvinceName"] as string;
                            obj["FullAddress"] = reader["full_address"] as string;
                            Response.Write(serializer.Serialize(obj));
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }
            }

            Response.Write(serializer.Serialize(new { error = "Không tìm thấy đại lý" }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.Write(serializer.Serialize(new { error = ex.Message }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}

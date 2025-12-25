using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

public partial class BookingsDetailApi : System.Web.UI.Page
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

        var orderId = (Request["orderId"] ?? string.Empty).Trim();
        var serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = int.MaxValue;

        if (string.IsNullOrWhiteSpace(orderId))
        {
            Response.Write(serializer.Serialize(new { error = "Missing orderId", data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
            return;
        }

        try
        {
            var items = new List<Dictionary<string, object>>();
            int totalCustomers = 0;
            decimal totalPrice = 0m;
            using (var conn = Db.CreateConnection())
            {
                conn.Open();
                using (var sumCmd = new SqlCommand("SELECT COUNT(*) AS TotalCustomers, SUM(ISNULL(GiaThucBan,0)) AS TotalPrice FROM customer WHERE orderID = @orderId AND visible = 1", conn))
                {
                    sumCmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    sumCmd.Parameters.Add(new SqlParameter("@orderId", orderId));
                    using (var reader = sumCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalCustomers = reader["TotalCustomers"] != DBNull.Value ? Convert.ToInt32(reader["TotalCustomers"]) : 0;
                            totalPrice = reader["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrice"]) : 0m;
                        }
                    }
                }
                var sql = @"
SELECT 
    c.Id,
    c.Fullname,
    c.LastName,
    c.Firstname,
    c.gender,
    c.tel,
    c.passport,
    c.GiaThucBan,
    c.Loaiphong,
    c.Loaigiuong
FROM customer c
WHERE c.orderID = @orderId AND c.visible = 1
ORDER BY c.Id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = Db.CommandTimeoutSeconds;
                    cmd.Parameters.Add(new SqlParameter("@orderId", orderId));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            row["CustomerName"] = BuildCustomerName(reader["Fullname"] as string, reader["LastName"] as string, reader["Firstname"] as string);
                            row["Gender"] = NormalizeGender(reader["gender"] as string);
                            row["Phone"] = reader["tel"] as string;
                            row["Passport"] = reader["passport"] as string;
                            row["Price"] = reader["GiaThucBan"];
                            row["Room"] = BuildRoom(reader["Loaiphong"] as string, reader["Loaigiuong"] as string);
                            items.Add(row);
                        }
                    }
                }
            }

            Response.Write(serializer.Serialize(new
            {
                data = items,
                summary = new
                {
                    OrderId = orderId,
                    TotalCustomers = totalCustomers,
                    TotalPrice = totalPrice
                }
            }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            Response.StatusCode = 200;
            Response.Write(serializer.Serialize(new { error = ex.Message, data = new object[0] }));
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }

    private string BuildCustomerName(string fullName, string lastName, string firstName)
    {
        if (!string.IsNullOrWhiteSpace(fullName)) return fullName.Trim();
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        return (lastName + " " + firstName).Trim();
    }

    private string NormalizeGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return gender;
        gender = gender.Trim();
        if (gender.Equals("F", StringComparison.OrdinalIgnoreCase)) return "Nữ";
        if (gender.Equals("M", StringComparison.OrdinalIgnoreCase)) return "Nam";
        return gender;
    }

    private string BuildRoom(string roomType, string bedType)
    {
        roomType = roomType != null ? roomType.Trim() : string.Empty;
        bedType = bedType != null ? bedType.Trim() : string.Empty;
        if (string.IsNullOrEmpty(roomType)) return bedType;
        if (string.IsNullOrEmpty(bedType)) return roomType;
        return roomType + " / " + bedType;
    }
}

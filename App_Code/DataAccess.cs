using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public static class Db
{
    public const int CommandTimeoutSeconds = 60;

    public static string ConnectionString
    {
        get
        {
            return ConfigurationManager.ConnectionStrings["CrmDb"].ConnectionString;
        }
    }

    public static SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}

public class UserRecord
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
}

public static class AuthService
{
    public static UserRecord ValidateUser(string username, string password)
    {
        const string sql = @"
            SELECT TOP 1 id, username, name, lastname12, Role
            FROM users
            WHERE username = @username AND password = @password";

        using (var conn = Db.CreateConnection())
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.CommandTimeout = Db.CommandTimeoutSeconds;
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new UserRecord
                    {
                        Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                        Username = reader["username"] as string,
                        Name = BuildFullName(reader["name"] as string, reader["lastname12"] as string),
                        Role = reader["Role"] as string
                    };
                }
            }
        }

        return null;
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        var full = (lastName + " " + firstName).Trim();
        return string.IsNullOrEmpty(full) ? null : full;
    }
}

public class BookingRecord
{
    public object OrderId { get; set; }
    public string CustomerName { get; set; }
    public string Gender { get; set; }
    public string Phone { get; set; }
    public string Source { get; set; }
    public string ProductName { get; set; }
    public string CreatedBy { get; set; }
    public decimal? Amount { get; set; }
    public decimal? AmountThucBan { get; set; }
    public DateTime? DepositDeadline { get; set; }
}

public static class BookingRepository
{
    public static IList<BookingRecord> GetBookings(BookingFilter filter)
    {
        var items = new List<BookingRecord>();

        var sql = @"
            SELECT 
                o.orderid,
                o.ctLastname,
                o.ctFirstname,
                o.ctgender,
                o.ctTel,
                o.ctnguon,
                o.Amount,
                o.Amountthucban,
                o.DepositDeadline,
                u.username AS CreatedBy,
                prod.Countries
            FROM [order] o
            LEFT JOIN users u ON u.id = o.userid
OUTER APPLY (
    SELECT TOP 1 STUFF((
        SELECT DISTINCT ', ' + d.name
        FROM customer c
        JOIN [product-des] pd ON pd.productID = c.ProductID
        JOIN destination d ON d.id = pd.destinationID
        WHERE c.orderID = o.orderid AND ISNULL(d.name,'') <> ''
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
) prod
            WHERE o.Visible = 1";

        var parameters = new List<SqlParameter>();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.OrderId))
            {
                sql += " AND CAST(o.orderid AS NVARCHAR(50)) LIKE @orderId";
                parameters.Add(new SqlParameter("@orderId", "%" + filter.OrderId.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                sql += " AND LOWER(ISNULL(o.ctLastname,'') + ' ' + ISNULL(o.ctFirstname,'')) LIKE @customerName";
                parameters.Add(new SqlParameter("@customerName", "%" + filter.CustomerName.Trim().ToLowerInvariant() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                sql += " AND o.ctTel LIKE @phone";
                parameters.Add(new SqlParameter("@phone", "%" + filter.Phone.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Source))
            {
                sql += " AND o.ctnguon LIKE @source";
                parameters.Add(new SqlParameter("@source", "%" + filter.Source.Trim() + "%"));
            }

            if (filter.BookingDate.HasValue)
            {
                sql += " AND CONVERT(date, o.DepositDeadline) = @bookingDate";
                parameters.Add(new SqlParameter("@bookingDate", filter.BookingDate.Value.Date));
            }
        }

        sql += " ORDER BY ";
        if (filter != null && filter.SortByAmount)
        {
            sql += " o.Amount DESC, o.Creationdate DESC, o.orderid DESC";
        }
        else
        {
            sql += " o.Creationdate DESC, o.orderid DESC";
        }

        using (var conn = Db.CreateConnection())
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.CommandTimeout = Db.CommandTimeoutSeconds;
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new BookingRecord
                    {
                        OrderId = reader["orderid"],
                        CustomerName = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string),
                        Gender = NormalizeGender(reader["ctgender"] as string),
                        Phone = reader["ctTel"] as string,
                        Source = reader["ctnguon"] as string,
                        ProductName = reader["Countries"] as string,
                        CreatedBy = NormalizeUsername(reader["CreatedBy"] as string),
                        Amount = reader["Amount"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Amount"]) : null,
                        AmountThucBan = reader["Amountthucban"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Amountthucban"]) : null,
                        DepositDeadline = reader["DepositDeadline"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DepositDeadline"]) : null
                    });
                }
            }
        }

        return items;
    }

    private static string BuildCustomerName(string lastName, string firstName)
    {
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        return (lastName + " " + firstName).Trim();
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return username;
        username = username.Trim();
        var atIndex = username.IndexOf('@');
        if (atIndex >= 0)
        {
            return username.Substring(0, atIndex);
        }
        return username;
    }

        private static string NormalizeGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return gender;
        gender = gender.Trim();
        if (gender.Equals("F", StringComparison.OrdinalIgnoreCase)) return "Nữ";
        if (gender.Equals("M", StringComparison.OrdinalIgnoreCase)) return "Nam";
        return gender;
    }}

public class BookingFilter
{
    public string OrderId { get; set; }
    public string CustomerName { get; set; }
    public string Phone { get; set; }
    public string Source { get; set; }
    public DateTime? BookingDate { get; set; }
    public bool SortByAmount { get; set; }
}

public class CustomerSummary
{
    public string Phone { get; set; }
    public string CustomerName { get; set; }
    public string Gender { get; set; }
    public int TotalBookings { get; set; }
    public DateTime? LatestCreation { get; set; }
    public string ProductName { get; set; }
    public decimal? TotalAmountThucBan { get; set; }
}

public static class CustomerRepository
{
    public static IList<CustomerSummary> GetCustomerSummaries(CustomerFilter filter = null)
    {
        var items = new List<CustomerSummary>();

        var sql = @"
WITH base AS (
    SELECT o.orderid,
           o.ctTel,
           LTRIM(RTRIM(o.ctTel)) AS CleanTel,
           o.ctLastname,
           o.ctFirstname,
           o.ctgender,
           o.ctnguon,
           o.Creationdate,
           o.Amountthucban
    FROM [order] o
    WHERE ISNULL(o.ctTel,'') <> ''
),
agg AS (
    SELECT CleanTel, COUNT(*) AS TotalBookings, MAX(Creationdate) AS LatestCreation, SUM(ISNULL(Amountthucban,0)) AS TotalAmountThucBan
    FROM base
    GROUP BY CleanTel
),
latest AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY CleanTel ORDER BY Creationdate DESC, orderid DESC) AS rn
    FROM base
)
SELECT 
    l.ctTel,
    l.ctLastname,
    l.ctFirstname,
    l.ctgender,
    l.ctnguon,
    a.TotalBookings,
    a.LatestCreation,
    a.TotalAmountThucBan,
    prod.Countries AS ProductName
FROM agg a
JOIN latest l ON l.CleanTel = a.CleanTel AND l.rn = 1
OUTER APPLY (
    SELECT TOP 1
        CASE 
            WHEN dests.Countries IS NOT NULL AND dests.Countries <> '' THEN dests.Countries
            ELSE prodNames.Names
        END AS Countries
    FROM (
        SELECT STUFF((
            SELECT DISTINCT ', ' + d.name
            FROM customer c
            JOIN [product-des] pd ON pd.productID = c.ProductID
            JOIN destination d ON d.id = pd.destinationID
            WHERE LTRIM(RTRIM(c.tel)) = a.CleanTel AND ISNULL(d.name, '') <> ''
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, '') AS Countries
    ) dests
    CROSS APPLY (
        SELECT STUFF((
            SELECT DISTINCT ', ' + ISNULL(p.name, '')
            FROM customer c2
            JOIN product p ON p.id = c2.ProductID
            WHERE LTRIM(RTRIM(c2.tel)) = a.CleanTel AND ISNULL(p.name,'') <> ''
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, '') AS Names
    ) prodNames
) prod
WHERE 1=1";

        var parameters = new List<SqlParameter>();
        int take = (filter != null && filter.MaxRows > 0) ? filter.MaxRows : 1000;
        sql = sql.Replace("SELECT \r\n    l.ctTel", "SELECT TOP (@take)\r\n    l.ctTel");
        parameters.Add(new SqlParameter("@take", take));

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                sql += " AND a.ctTel LIKE @phone";
                parameters.Add(new SqlParameter("@phone", "%" + filter.Phone.Trim() + "%"));
            }
            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                sql += " AND LOWER(ISNULL(l.ctLastname,'') + ' ' + ISNULL(l.ctFirstname,'')) LIKE @customerName";
                parameters.Add(new SqlParameter("@customerName", "%" + filter.CustomerName.Trim().ToLowerInvariant() + "%"));
            }
            if (!string.IsNullOrWhiteSpace(filter.Source))
            {
                sql += " AND l.ctnguon LIKE @source";
                parameters.Add(new SqlParameter("@source", "%" + filter.Source.Trim() + "%"));
            }
            if (filter.BookingDate.HasValue)
            {
                sql += " AND CONVERT(date, a.LatestCreation) = @latestDate";
                parameters.Add(new SqlParameter("@latestDate", filter.BookingDate.Value.Date));
            }
        }

        sql += " ORDER BY a.LatestCreation DESC, a.TotalBookings DESC OPTION (RECOMPILE)";

        using (var conn = Db.CreateConnection())
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.CommandTimeout = Db.CommandTimeoutSeconds;
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new CustomerSummary
                    {
                        Phone = reader["ctTel"] as string,
                        CustomerName = BuildCustomerName(reader["ctLastname"] as string, reader["ctFirstname"] as string),
                        Gender = NormalizeGender(reader["ctgender"] as string),
                        ProductName = reader["ProductName"] as string,
                        TotalBookings = reader["TotalBookings"] != DBNull.Value ? Convert.ToInt32(reader["TotalBookings"]) : 0,
                        LatestCreation = reader["LatestCreation"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["LatestCreation"]) : null,
                        TotalAmountThucBan = reader["TotalAmountThucBan"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["TotalAmountThucBan"]) : null
                    });
                }
            }
        }

        return items;
    }

    private static string BuildCustomerName(string lastName, string firstName)
    {
        lastName = lastName != null ? lastName.Trim() : string.Empty;
        firstName = firstName != null ? firstName.Trim() : string.Empty;
        return (lastName + " " + firstName).Trim();
    }

        private static string NormalizeGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return gender;
        gender = gender.Trim();
        if (gender.Equals("F", StringComparison.OrdinalIgnoreCase)) return "Nữ";
        if (gender.Equals("M", StringComparison.OrdinalIgnoreCase)) return "Nam";
        return gender;
    }}

public class CustomerFilter
{
    public string Phone { get; set; }
    public string CustomerName { get; set; }
    public string Source { get; set; }
    public DateTime? BookingDate { get; set; }
    public bool SortByRecent { get; set; }
    public int MaxRows { get; set; }
}

using System;
public partial class Bookings : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            var role = Session["Role"] as string;
            bool isAdmin = !string.IsNullOrEmpty(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);
            if (btnExportExcel != null)
            {
                btnExportExcel.Visible = isAdmin;
            }
        }
    }
}

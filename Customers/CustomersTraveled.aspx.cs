using System;

public partial class CustomersTraveled : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            var role = Session["Role"] as string;
            bool isAdmin = !string.IsNullOrEmpty(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);
            if (btnExport != null)
            {
                btnExport.Visible = isAdmin;
            }
        }
    }
}

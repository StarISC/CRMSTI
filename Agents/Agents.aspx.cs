using System;

public partial class Agents : BasePage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            var role = Session["Role"] as string;
            bool isAdmin = !string.IsNullOrEmpty(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);
            if (btnAddAgent != null)
            {
                btnAddAgent.Visible = isAdmin;
            }
        }
    }
}

using System;
using System.Web.Security;
using System.Globalization;

public partial class Customers : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            FormsAuthentication.SignOut();
            Response.Redirect("~/Login.aspx");
            return;
        }
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

using System;
using System.Web.Security;

public partial class Bookings : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            FormsAuthentication.SignOut();
            Response.Redirect("~/Login.aspx");
            return;
        }
    }
}

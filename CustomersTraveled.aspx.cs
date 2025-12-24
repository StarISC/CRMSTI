using System;

public partial class CustomersTraveled : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Response.Redirect("~/Login.aspx");
            return;
        }
    }
}

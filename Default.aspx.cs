using System;

public partial class RootDefault : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Redirect("~/Dashboard/Default.aspx");
    }
}

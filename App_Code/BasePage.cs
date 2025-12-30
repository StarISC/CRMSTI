using System;
using System.Linq;
using System.Web;

public class BasePage : System.Web.UI.Page
{
    protected virtual bool RequireLogin
    {
        get { return true; }
    }

    protected virtual string[] AllowedRoles
    {
        get { return null; }
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        if (!RequireLogin)
        {
            return;
        }

        if (Session["UserId"] == null)
        {
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                System.Web.Security.FormsAuthentication.SignOut();
                Session.Abandon();
            }
            Response.Redirect("~/Login.aspx");
            return;
        }

        if (AllowedRoles != null && AllowedRoles.Length > 0)
        {
            var role = Session["Role"] as string;
            if (string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Dashboard/Default.aspx");
            }
        }
    }
}

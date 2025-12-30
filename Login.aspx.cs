using System;
using System.Web.Security;

public partial class Login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack && User != null && User.Identity.IsAuthenticated && Session["UserId"] != null)
        {
            Response.Redirect("~/Dashboard/Default.aspx");
        }
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            litMessage.Text = "Vui lòng nhập tài khoản và mật khẩu.";
            return;
        }

        var user = AuthService.ValidateUser(username, password);
        if (user != null)
        {
            Session["UserId"] = user.Id;
            Session["Username"] = user.Username;
            Session["UserDisplayName"] = string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name;
            Session["Role"] = user.Role;
            bool persistent = chkRemember.Checked;
            FormsAuthentication.SetAuthCookie(user.Username, persistent);
            Response.Redirect("~/Dashboard/Default.aspx");
        }
        else
        {
            litMessage.Text = "Sai tài khoản hoặc mật khẩu.";
        }
    }
}

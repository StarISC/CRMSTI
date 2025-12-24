using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;

public partial class SiteMaster : MasterPage
{
    private const string DashboardClass = "nav-link stacked d-flex flex-column align-items-center";
    private const string CustomersClass = "nav-link dropdown-toggle stacked d-flex flex-column align-items-center";
    private const string ConsultingClass = "nav-link stacked d-flex flex-column align-items-center";

    protected void Page_Load(object sender, EventArgs e)
    {
        var displayName = (Session["UserDisplayName"] as string) ?? (Session["Username"] as string);
        litUserName.Text = displayName ?? string.Empty;
        SetActiveMenu();
    }

    protected void Logout_Click(object sender, EventArgs e)
    {
        Session.Clear();
        FormsAuthentication.SignOut();
        Response.Redirect("~/Login.aspx");
    }

    private void SetActiveMenu()
    {
        ResetMenuClasses();

        var path = VirtualPathUtility.ToAppRelative(Request.Path).ToLowerInvariant();
        if (path.Contains("bookings.aspx") || path.Contains("customers.aspx") || path.Contains("customerstraveled.aspx"))
        {
            SetActive(lnkCustomersToggle, CustomersClass);
        }
        else if (path.Contains("consulting.aspx"))
        {
            SetActive(lnkConsulting, ConsultingClass);
        }
        else
        {
            SetActive(lnkDashboard, DashboardClass);
        }
    }

    private void ResetMenuClasses()
    {
        if (lnkDashboard != null)
        {
            lnkDashboard.Attributes["class"] = DashboardClass;
        }

        if (lnkCustomersToggle != null)
        {
            lnkCustomersToggle.Attributes["class"] = CustomersClass;
        }

        if (lnkConsulting != null)
        {
            lnkConsulting.Attributes["class"] = ConsultingClass;
        }
    }

    private void SetActive(HtmlAnchor anchor, string baseClass)
    {
        if (anchor == null) return;
        anchor.Attributes["class"] = baseClass + " active";
    }
}

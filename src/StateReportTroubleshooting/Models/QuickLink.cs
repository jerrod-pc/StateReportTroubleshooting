namespace StateReportTroubleshooting.Models;

public record QuickLink(string Label, string Url);

public static class QuickLinks
{
    public static readonly List<QuickLink> All =
    [
        new("MA Education Security Portal Login", "https://gateway.edu.state.ma.us/stardust/login"),
        new("DESE Data Collection Documentation", "https://www.doe.mass.edu/infoservices/data/default.html"),
        new("EOE Support Request Form", "https://massgov.service-now.com/eoe?id=eoe_req_form&sys_id=87451de4dbba47006152f25bbf961923"),
        new("Rediker Software State Reporting Solutions", "https://support.rediker.com/en/support/solutions/14000072051"),
    ];
}

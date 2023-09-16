using System.ComponentModel.DataAnnotations;

namespace Milad.IdentityServer.Pages.Admin.ApiScopes;

public class ApiScopeSummaryModel
{
    [Required] public string Name { get; set; }

    public string DisplayName { get; set; }
}
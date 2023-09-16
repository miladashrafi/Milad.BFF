using System.ComponentModel.DataAnnotations;

namespace Milad.IdentityServer.Pages.Admin.IdentityScopes;

public class IdentityScopeSummaryModel
{
    [Required] public string Name { get; set; }

    public string DisplayName { get; set; }
}
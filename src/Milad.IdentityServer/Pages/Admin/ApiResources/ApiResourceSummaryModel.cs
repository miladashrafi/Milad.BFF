using System.ComponentModel.DataAnnotations;

namespace Milad.IdentityServer.Pages.Admin.ApiResources;

public class ApiResourceSummaryModel
{
    [Required] public string Name { get; set; }

    public string DisplayName { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace Milad.IdentityServer.Pages.Admin.Clients;

public class ClientSummaryModel
{
    [Required] public string ClientId { get; set; }

    public string Name { get; set; }

    [Required] public Flow Flow { get; set; }
}
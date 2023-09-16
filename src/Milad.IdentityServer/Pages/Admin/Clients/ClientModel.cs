using System.ComponentModel.DataAnnotations;

namespace Milad.IdentityServer.Pages.Admin.Clients;

public class ClientModel : CreateClientModel, IValidatableObject
{
    [Required] public string AllowedScopes { get; set; }

    public string RedirectUri { get; set; }
    public string PostLogoutRedirectUri { get; set; }
    public string FrontChannelLogoutUri { get; set; }
    public string BackChannelLogoutUri { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        if (Flow == Flow.CodeFlowWithPkce)
            if (RedirectUri == null)
                errors.Add(new ValidationResult("Redirect URI is required.", new[] { "RedirectUri" }));

        return errors;
    }
}
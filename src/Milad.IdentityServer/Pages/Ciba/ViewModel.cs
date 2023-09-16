// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Milad.IdentityServer.Pages.Ciba;

public class ViewModel
{
    public string ClientName { get; set; }
    public string ClientUrl { get; set; }
    public string ClientLogoUrl { get; set; }

    public string BindingMessage { get; set; }

    public IEnumerable<ScopeViewModel> IdentityScopes { get; set; }
    public IEnumerable<ScopeViewModel> ApiScopes { get; set; }
}
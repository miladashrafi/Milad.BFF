using Duende.IdentityServer.Models;

namespace Milad.IdentityServer;

public static class Config
{
    private static List<string> AllIdentityScopes =>
        IdentityResources.Select(s => s.Name).ToList();

    private static List<string> AllApiScopes =>
        ApiScopes.Select(s => s.Name).ToList();

    private static List<string> AllScopes =>
        AllApiScopes.Concat(AllIdentityScopes).ToList();

    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            // backward compat
            new("api"),

            // resource specific scopes
            new("resource1.scope1"),
            new("resource1.scope2"),

            new("resource2.scope1"),
            new("resource2.scope2"),

            new("resource3.scope1"),
            new("resource3.scope2"),

            // a scope without resource association
            new("scope3"),
            new("scope4"),

            // a scope shared by multiple resources
            new("shared.scope"),

            // a parameterized scope
            new("transaction", "Transaction")
            {
                Description = "Some Transaction"
            }
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new("api", "Demo API", new[] { "name", "email" })
            {
                ApiSecrets = { new Secret("secret".Sha256()) },

                Scopes = { "api" }
            },

            new("urn:resource1", "Resource 1")
            {
                ApiSecrets = { new Secret("secret".Sha256()) },

                Scopes = { "resource1.scope1", "resource1.scope2", "shared.scope" }
            },

            new("urn:resource2", "Resource 2")
            {
                ApiSecrets = { new Secret("secret".Sha256()) },

                Scopes = { "resource2.scope1", "resource2.scope2", "shared.scope" }
            },

            new("urn:resource3", "Resource 3 (isolated)")
            {
                ApiSecrets = { new Secret("secret".Sha256()) },
                RequireResourceIndicator = true,

                Scopes = { "resource3.scope1", "resource3.scope2", "shared.scope" }
            }
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // interactive client using code flow + pkce
            new()
            {
                ClientId = "interactive.confidential",
                ClientName = "Interactive client (Code with PKCE)",

                RedirectUris = { "https://localhost:5002/signin-oidc" },
                PostLogoutRedirectUris = { "https://localhost:5002" },

                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                AllowedScopes = AllScopes,

                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding
            }
        };
}
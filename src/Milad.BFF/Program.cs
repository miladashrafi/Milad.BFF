using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.IdentityModel.Tokens;
using Milad.BFF;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBff()
    .AddRemoteApis();

builder.Services.AddTransient<IReturnUrlValidator, FrontendHostReturnUrlValidator>();

Configuration config = new();
builder.Configuration.Bind("BFF", config);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme = "oidc";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-bff";
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = config.Authority;
        options.ClientId = config.ClientId;
        options.ClientSecret = config.ClientSecret;

        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;

        options.Scope.Clear();
        foreach (var scope in config.Scopes) options.Scope.Add(scope);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// builder.Services.AddAuthorization();


// add CORS policy for non-IdentityServer endpoints
builder.Services.AddCors(options =>
{
    options.AddPolicy("allow_all",
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("allow_all");
app.UseAuthentication();
app.UseBff();
// app.UseAuthorization();

app.MapBffManagementEndpoints();

if (config.Apis.Any())
    foreach (var api in config.Apis)
        app.MapRemoteBffApiEndpoint(api.LocalPath, api.RemoteUrl!)
            .RequireAccessToken(api.RequiredToken);

app.Run();
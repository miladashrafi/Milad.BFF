using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Milad.IdentityServer.Data;
using Milad.IdentityServer.Models;
using Milad.IdentityServer.Pages.Admin.ApiScopes;
using Milad.IdentityServer.Pages.Admin.IdentityScopes;
using Milad.IdentityServer.Pages.Portal;
using Serilog;

namespace Milad.IdentityServer;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        IdentityModelEventSource.ShowPII = true;
        builder.Services.AddRazorPages();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.EnableRetryOnFailure()));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole<long>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var isBuilder = builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
            // this adds the config data from DB (clients, resources, CORS)
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(connectionString, dbOpts => dbOpts
                        .MigrationsAssembly(typeof(Program).Assembly.FullName)
                        .EnableRetryOnFailure());
            })
            // this is something you will want in production to reduce load on and requests to the DB
            //.AddConfigurationStoreCache()
            //
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(connectionString, dbOpts => dbOpts
                        .MigrationsAssembly(typeof(Program).Assembly.FullName)
                        .EnableRetryOnFailure());
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddJwtBearerClientAuthentication();

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                // register your IdentityServer with Google at https://console.developers.google.com
                // enable the Google+ API
                // set the redirect URI to https://localhost:5001/signin-google
                options.ClientId = "copy client ID from Google here";
                options.ClientSecret = "copy client secret from Google here";
            });

        // add CORS policy for non-IdentityServer endpoints
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("allow_all",
                policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
        });

        // this adds the necessary config for the simple admin/config pages
        {
            builder.Services.AddAuthorization(options =>
                options.AddPolicy("admin", policy => policy.RequireRole("admin"))
            );

            builder.Services.Configure<RazorPagesOptions>(options =>
                options.Conventions.AuthorizeFolder("/Admin", "admin"));

            builder.Services.AddTransient<ClientRepository>();
            builder.Services.AddTransient<Pages.Admin.Clients.ClientRepository>();
            builder.Services.AddTransient<IdentityScopeRepository>();
            builder.Services.AddTransient<ApiScopeRepository>();
        }

        // if you want to use server-side sessions: https://blog.duendesoftware.com/posts/20220406_session_management/
        // then enable it
        //isBuilder.AddServerSideSessions();
        //
        // and put some authorization on the admin/management pages using the same policy created above
        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));

        return builder.Build();
    }

    public static async Task<WebApplication> ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

        await InitializeDatabase(app);

        app.UseCors("allow_all");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }

    private static async Task InitializeDatabase(IApplicationBuilder app)
    {
        await using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateAsyncScope())
        {
            await serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            await context.Database.MigrateAsync();
            var applicationDbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
            await applicationDbContext.Database.MigrateAsync();
            
            if (!await context.IdentityResources.AnyAsync())
            {
                foreach (var resource in Config.IdentityResources) context.IdentityResources.Add(resource.ToEntity());
                await context.SaveChangesAsync();
            }

            if (!await context.ApiScopes.AnyAsync())
            {
                foreach (var resource in Config.ApiScopes) context.ApiScopes.Add(resource.ToEntity());
                await context.SaveChangesAsync();
            }

            if (!await context.ApiResources.AnyAsync())
            {
                foreach (var resource in Config.ApiResources) context.ApiResources.Add(resource.ToEntity());
                await context.SaveChangesAsync();
            }
            
            if (!await context.Clients.AnyAsync())
            {
                foreach (var client in Config.Clients) context.Clients.Add(client.ToEntity());
                await context.SaveChangesAsync();
            }

            var userMgr = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleMgr = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

            if (await roleMgr.FindByNameAsync("admin") is null)
            {
                var role = new IdentityRole<long>("admin");
                var result = await roleMgr.CreateAsync(role);
                if (!result.Succeeded) throw new Exception(result.Errors.First().Description);
            }

            var admin = await userMgr.FindByNameAsync("09127372975");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "09127372975",
                    Email = "milad.ashrafi@gmail.com",
                    EmailConfirmed = true
                };
                var result = await userMgr.CreateAsync(admin, "Mil@d123");
                if (!result.Succeeded) throw new Exception(result.Errors.First().Description);

                var addRoleResult = await userMgr.AddToRoleAsync(admin, "admin");
                if (!addRoleResult.Succeeded) throw new Exception(addRoleResult.Errors.First().Description);

                result = await userMgr.AddClaimsAsync(admin, new Claim[]
                {
                    new(JwtClaimTypes.Name, "Milad Ashrafi"),
                    new(JwtClaimTypes.GivenName, "Milad"),
                    new(JwtClaimTypes.FamilyName, "Ashrafi"),
                    new(JwtClaimTypes.Role, "admin"),
                    new(JwtClaimTypes.WebSite, "https://binande.ir")
                });
                if (!result.Succeeded) throw new Exception(result.Errors.First().Description);
                Log.Debug("admin created");
            }
            else
            {
                Log.Debug("admin already exists");
            }
        }
    }
}
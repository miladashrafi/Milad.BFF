using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using ApiResource = Duende.IdentityServer.Models.ApiResource;

namespace Milad.IdentityServer.Pages.Admin.ApiResources;

public class ApiResourceRepository
{
    private readonly ConfigurationDbContext _context;

    public ApiResourceRepository(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApiResourceSummaryModel>> GetAllAsync(string filter = null)
    {
        var query = _context.ApiResources
            .Include(x => x.UserClaims)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(x => x.Name.Contains(filter) || x.DisplayName.Contains(filter));

        var result = query.Select(x => new ApiResourceSummaryModel
        {
            Name = x.Name,
            DisplayName = x.DisplayName
        });

        return await result.ToArrayAsync();
    }

    public async Task<ApiResourceModel> GetByIdAsync(string id)
    {
        var scope = await _context.ApiResources
            .Include(x => x.UserClaims)
            .SingleOrDefaultAsync(x => x.Name == id);

        if (scope == null) return null;

        return new ApiResourceModel
        {
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            UserClaims = scope.UserClaims.Any()
                ? scope.UserClaims.Select(x => x.Type).Aggregate((a, b) => $"{a} {b}")
                : null
        };
    }

    public async Task CreateAsync(ApiResourceModel model)
    {
        var scope = new ApiResource
        {
            Name = model.Name,
            DisplayName = model.DisplayName?.Trim()
        };

        var claims = model.UserClaims?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray() ??
                     Enumerable.Empty<string>();
        if (claims.Any()) scope.UserClaims = claims.ToList();

        _context.ApiResources.Add(scope.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ApiResourceModel model)
    {
        var scope = await _context.ApiResources
            .Include(x => x.UserClaims)
            .SingleOrDefaultAsync(x => x.Name == model.Name);

        if (scope == null) throw new Exception("Invalid Identity Scope");

        if (scope.DisplayName != model.DisplayName) scope.DisplayName = model.DisplayName?.Trim();

        var claims = model.UserClaims?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray() ??
                     Enumerable.Empty<string>();
        var currentClaims = (scope.UserClaims.Select(x => x.Type) ?? Enumerable.Empty<string>()).ToArray();

        var claimsToAdd = claims.Except(currentClaims).ToArray();
        var claimsToRemove = currentClaims.Except(claims).ToArray();

        if (claimsToRemove.Any()) scope.UserClaims.RemoveAll(x => claimsToRemove.Contains(x.Type));
        if (claimsToAdd.Any())
            scope.UserClaims.AddRange(claimsToAdd.Select(x => new ApiResourceClaim
            {
                Type = x
            }));

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var scope = await _context.ApiResources.SingleOrDefaultAsync(x => x.Name == id);

        if (scope == null) throw new Exception("Invalid Identity Scope");

        _context.ApiResources.Remove(scope);
        await _context.SaveChangesAsync();
    }
}
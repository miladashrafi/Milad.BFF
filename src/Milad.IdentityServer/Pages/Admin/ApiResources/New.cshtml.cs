using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Milad.IdentityServer.Pages.Admin.ApiResources;

[SecurityHeaders]
[Authorize]
public class NewModel : PageModel
{
    private readonly ApiResourceRepository _repository;

    public NewModel(ApiResourceRepository repository)
    {
        _repository = repository;
    }

    [BindProperty] public ApiResourceModel InputModel { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            await _repository.CreateAsync(InputModel);
            return RedirectToPage("/Admin/ApiResources/Edit", new { id = InputModel.Name });
        }

        return Page();
    }
}
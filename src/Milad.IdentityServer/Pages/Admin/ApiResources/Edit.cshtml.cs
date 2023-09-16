using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Milad.IdentityServer.Pages.Admin.ApiResources;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly ApiResourceRepository _repository;

    public EditModel(ApiResourceRepository repository)
    {
        _repository = repository;
    }

    [BindProperty] public ApiResourceModel InputModel { get; set; }

    [BindProperty] public string Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        InputModel = await _repository.GetByIdAsync(id);
        if (InputModel == null) return RedirectToPage("/Admin/ApiResources/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (Button == "delete")
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage("/Admin/ApiResources/Index");
        }

        if (ModelState.IsValid)
        {
            await _repository.UpdateAsync(InputModel);
            return RedirectToPage("/Admin/ApiResources/Edit", new { id });
        }

        return Page();
    }
}
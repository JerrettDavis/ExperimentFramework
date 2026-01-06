using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspireDemo.Web.Pages;

[Authorize(Policy = "CanAccessExperiments")]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/dashboard");
    }
}

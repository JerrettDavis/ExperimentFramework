using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExperimentFramework.DashboardHost.Pages;

/// <summary>
/// Minimal login stub for docs-demo mode. Auth is disabled so any credentials
/// are accepted and the user is redirected to /dashboard immediately.
/// </summary>
public class LoginModel : PageModel
{
    public IActionResult OnGet() => Page();

    public IActionResult OnPost()
    {
        // Docs-demo mode: no real auth — accept any credentials and redirect
        return Redirect("/dashboard");
    }
}

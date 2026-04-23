using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ExperimentFramework.DashboardHost.Pages;

/// <summary>
/// Login page for the docs-demo host. Validates against a set of known demo users
/// and issues a cookie-based authentication ticket on success.
/// </summary>
public class LoginModel : PageModel
{
    /// <summary>Demo users recognised by the stub host (email → role).</summary>
    private static readonly Dictionary<string, (string Password, string Role)> KnownUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@experimentdemo.com"]        = ("Admin123!",        "Admin"),
        ["experimenter@experimentdemo.com"] = ("Experimenter123!", "Experimenter"),
        ["viewer@experimentdemo.com"]       = ("Viewer123!",       "Viewer"),
        ["analyst@experimentdemo.com"]      = ("Analyst123!",      "Analyst"),
    };

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        ReturnUrl ??= Url.Content("/dashboard");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? email, string? password, bool rememberMe)
    {
        var returnUrl = ReturnUrl ?? "/dashboard";
        Email = email;
        RememberMe = rememberMe;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        if (!KnownUsers.TryGetValue(email.Trim(), out var creds) ||
            creds.Password != password)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // Issue the auth cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,  email.Trim()),
            new(ClaimTypes.Email, email.Trim()),
            new(ClaimTypes.Role,  creds.Role),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc   = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8),
                RedirectUri  = returnUrl,
            });

        if (Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return Redirect("/dashboard");
    }
}

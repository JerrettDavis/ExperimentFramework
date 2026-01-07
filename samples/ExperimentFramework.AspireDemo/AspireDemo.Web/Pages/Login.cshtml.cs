using AspireDemo.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AspireDemo.Web.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }

    private static string SanitizeForLog(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Replace("\r", "").Replace("\n", "");
    }

    public void OnGet()
    {
        _logger.LogInformation("========== Login.OnGet START ==========");
        _logger.LogInformation("Login.OnGet - Request Path: {Path}", SanitizeForLog(HttpContext.Request.Path.Value));
        _logger.LogInformation("Login.OnGet - Query String: {QueryString}", SanitizeForLog(HttpContext.Request.QueryString.Value));
        _logger.LogInformation("Login.OnGet - Error: {Error}, ReturnUrl: {ReturnUrl}", Error, ReturnUrl);
        _logger.LogInformation("Login.OnGet - User authenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
        _logger.LogInformation("========== Login.OnGet END ==========");

        if (!string.IsNullOrEmpty(Error))
        {
            ErrorMessage = Error switch
            {
                "locked" => "Account is locked out. Please try again later.",
                "invalid" => "Invalid email or password.",
                _ => "An error occurred during login."
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("========== Login.OnPostAsync START ==========");
        _logger.LogInformation("OnPostAsync - Request Path: {Path}", SanitizeForLog(HttpContext.Request.Path.Value));
        _logger.LogInformation("OnPostAsync - Request Method: {Method}", SanitizeForLog(HttpContext.Request.Method));
        _logger.LogInformation("OnPostAsync - Content-Type: {ContentType}", HttpContext.Request.ContentType);
        _logger.LogInformation("OnPostAsync - Has Form: {HasForm}", HttpContext.Request.HasFormContentType);
        _logger.LogInformation("OnPostAsync - Email: {Email}", Email);
        _logger.LogInformation("OnPostAsync - RememberMe: {RememberMe}", RememberMe);
        _logger.LogInformation("OnPostAsync - ReturnUrl: {ReturnUrl}", ReturnUrl);
        _logger.LogInformation("OnPostAsync - ModelState valid: {IsValid}", ModelState.IsValid);
        _logger.LogInformation("OnPostAsync - User authenticated before sign in: {IsAuthenticated}", User.Identity?.IsAuthenticated);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("OnPostAsync - ModelState is invalid");
            foreach (var error in ModelState)
            {
                _logger.LogWarning("OnPostAsync - ModelState error: {Key} - {Errors}",
                    error.Key, string.Join(", ", error.Value?.Errors.Select(e => e.ErrorMessage) ?? Array.Empty<string>()));
            }
        }

        _logger.LogInformation("OnPostAsync - About to call PasswordSignInAsync");

        var result = await _signInManager.PasswordSignInAsync(
            Email,
            Password,
            RememberMe,
            lockoutOnFailure: true);

        _logger.LogInformation("OnPostAsync - PasswordSignInAsync completed");
        _logger.LogInformation("OnPostAsync - Result: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, RequiresTwoFactor={RequiresTwoFactor}, IsNotAllowed={IsNotAllowed}",
            result.Succeeded, result.IsLockedOut, result.RequiresTwoFactor, result.IsNotAllowed);

        if (result.Succeeded)
        {
            _logger.LogInformation("OnPostAsync - Login succeeded!");
            _logger.LogInformation("OnPostAsync - User authenticated after sign in: {IsAuthenticated}", User.Identity?.IsAuthenticated);
            _logger.LogInformation("OnPostAsync - User identity name: {Name}", User.Identity?.Name);

            var redirectUrl = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/dashboard";
            _logger.LogInformation("OnPostAsync - Preparing LocalRedirect to: {RedirectUrl}", redirectUrl);
            _logger.LogInformation("OnPostAsync - Response has started: {HasStarted}", HttpContext.Response.HasStarted);

            var redirectResult = LocalRedirect(redirectUrl);
            _logger.LogInformation("OnPostAsync - LocalRedirect result created");
            _logger.LogInformation("========== Login.OnPostAsync END (SUCCESS) ==========");
            return redirectResult;
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("OnPostAsync - Account is locked out for email: {Email}", Email);
            var redirectResult = RedirectToPage("/Account/Login", new { error = "locked", returnUrl = ReturnUrl });
            _logger.LogInformation("========== Login.OnPostAsync END (LOCKED OUT) ==========");
            return redirectResult;
        }
        else
        {
            _logger.LogWarning("OnPostAsync - Invalid login attempt for email: {Email}", Email);
            _logger.LogWarning("OnPostAsync - Is not allowed: {IsNotAllowed}", result.IsNotAllowed);

            // Check if user exists
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                _logger.LogWarning("OnPostAsync - User not found in database");
            }
            else
            {
                _logger.LogInformation("OnPostAsync - User found. Email confirmed: {EmailConfirmed}, Lockout enabled: {LockoutEnabled}",
                    user.EmailConfirmed, user.LockoutEnabled);
            }

            ErrorMessage = "Invalid email or password.";
            _logger.LogInformation("========== Login.OnPostAsync END (FAILED) ==========");
            return Page();
        }
    }
}

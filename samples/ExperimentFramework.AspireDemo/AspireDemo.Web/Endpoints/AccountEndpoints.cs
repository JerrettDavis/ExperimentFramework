using AspireDemo.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspireDemo.Web.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/account");

        group.MapPost("/login", async (
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] bool? rememberMe,
            [FromQuery] string? returnUrl,
            SignInManager<ApplicationUser> signInManager,
            HttpContext httpContext) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                email,
                password,
                rememberMe ?? false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var redirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/dashboard";
                return Results.Redirect(redirectUrl);
            }
            else if (result.IsLockedOut)
            {
                return Results.Redirect($"/Account/Login?error=locked&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
            }
            else
            {
                return Results.Redirect($"/Account/Login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
            }
        })
        .DisableAntiforgery();

        group.MapPost("/logout", async (
            SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/Account/Login");
        })
        .RequireAuthorization();
    }
}

using Microsoft.AspNetCore.Identity;

namespace AspireDemo.Web.Data;

/// <summary>
/// Seeds the database with roles and test users.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Seeds roles and test users.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        string[] roles = { "Admin", "Experimenter", "Viewer", "Analyst" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create admin user
        if (await userManager.FindByEmailAsync("admin@experimentdemo.com") == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@experimentdemo.com",
                Email = "admin@experimentdemo.com",
                EmailConfirmed = true,
                FullName = "Admin User",
                Department = "Engineering",
                CanAccessExperiments = true,
                CanModifyExperiments = true,
                CanManageRollouts = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Create experimenter user
        if (await userManager.FindByEmailAsync("experimenter@experimentdemo.com") == null)
        {
            var experimenterUser = new ApplicationUser
            {
                UserName = "experimenter@experimentdemo.com",
                Email = "experimenter@experimentdemo.com",
                EmailConfirmed = true,
                FullName = "Experimenter User",
                Department = "Product",
                CanAccessExperiments = true,
                CanModifyExperiments = true,
                CanManageRollouts = true
            };

            var result = await userManager.CreateAsync(experimenterUser, "Experimenter123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(experimenterUser, "Experimenter");
            }
        }

        // Create viewer user
        if (await userManager.FindByEmailAsync("viewer@experimentdemo.com") == null)
        {
            var viewerUser = new ApplicationUser
            {
                UserName = "viewer@experimentdemo.com",
                Email = "viewer@experimentdemo.com",
                EmailConfirmed = true,
                FullName = "Viewer User",
                Department = "Marketing",
                CanAccessExperiments = true,
                CanModifyExperiments = false,
                CanManageRollouts = false
            };

            var result = await userManager.CreateAsync(viewerUser, "Viewer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(viewerUser, "Viewer");
            }
        }

        // Create analyst user
        if (await userManager.FindByEmailAsync("analyst@experimentdemo.com") == null)
        {
            var analystUser = new ApplicationUser
            {
                UserName = "analyst@experimentdemo.com",
                Email = "analyst@experimentdemo.com",
                EmailConfirmed = true,
                FullName = "Analyst User",
                Department = "Data Science",
                CanAccessExperiments = true,
                CanModifyExperiments = false,
                CanManageRollouts = false
            };

            var result = await userManager.CreateAsync(analystUser, "Analyst123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(analystUser, "Analyst");
            }
        }
    }
}

using AspireDemo.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AspireDemo.Web.Tests;

[TestFixture]
public class IdentitySeedingTests
{
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();

        // Add in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("DataSource=:memory:"));

        // Add Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();

        // Create database schema
        var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.OpenConnection();
        dbContext.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.CloseConnection();
        _serviceProvider.Dispose();
    }

    [Test]
    public async Task SeedAsync_CreatesAdminUser()
    {
        // Arrange & Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@experimentdemo.com");

        Assert.That(adminUser, Is.Not.Null, "Admin user should exist");
        Assert.That(adminUser!.UserName, Is.EqualTo("admin@experimentdemo.com"));
        Assert.That(adminUser.Email, Is.EqualTo("admin@experimentdemo.com"));
        Assert.That(adminUser.EmailConfirmed, Is.True);
        Assert.That(adminUser.FullName, Is.EqualTo("Admin User"));
        Assert.That(adminUser.CanAccessExperiments, Is.True);
        Assert.That(adminUser.CanModifyExperiments, Is.True);
        Assert.That(adminUser.CanManageRollouts, Is.True);
    }

    [Test]
    public async Task SeedAsync_AdminUserHasPasswordHash()
    {
        // Arrange & Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@experimentdemo.com");

        Assert.That(adminUser, Is.Not.Null);
        Assert.That(adminUser!.PasswordHash, Is.Not.Null.And.Not.Empty, "Password hash should be set");

        Console.WriteLine($"Password hash: {adminUser.PasswordHash}");
    }

    [Test]
    public async Task SeedAsync_AdminUserPasswordVerifies()
    {
        // Arrange & Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@experimentdemo.com");

        Assert.That(adminUser, Is.Not.Null);

        var isPasswordValid = await userManager.CheckPasswordAsync(adminUser!, "Admin123!");
        Assert.That(isPasswordValid, Is.True, "Password 'Admin123!' should be valid for admin user");
    }

    [Test]
    public async Task SeedAsync_AdminUserInAdminRole()
    {
        // Arrange & Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@experimentdemo.com");

        Assert.That(adminUser, Is.Not.Null);

        var roles = await userManager.GetRolesAsync(adminUser!);
        Assert.That(roles, Contains.Item("Admin"));
    }

    [Test]
    public async Task SeedAsync_CreatesAllFourUsers()
    {
        // Arrange & Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByEmailAsync("admin@experimentdemo.com");
        var experimenter = await userManager.FindByEmailAsync("experimenter@experimentdemo.com");
        var viewer = await userManager.FindByEmailAsync("viewer@experimentdemo.com");
        var analyst = await userManager.FindByEmailAsync("analyst@experimentdemo.com");

        Assert.That(admin, Is.Not.Null, "Admin user should exist");
        Assert.That(experimenter, Is.Not.Null, "Experimenter user should exist");
        Assert.That(viewer, Is.Not.Null, "Viewer user should exist");
        Assert.That(analyst, Is.Not.Null, "Analyst user should exist");
    }
}

using AspireDemo.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace AspireDemo.Web.Tests;

[TestFixture]
public class SignInTests
{
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

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
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add HTTP context accessor (required for SignInManager)
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

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
    public async Task PasswordSignInAsync_WithValidCredentials_Succeeds()
    {
        // Arrange
        await IdentitySeeder.SeedAsync(_serviceProvider);
        var signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        // Act
        var result = await signInManager.PasswordSignInAsync(
            "admin@experimentdemo.com",
            "Admin123!",
            isPersistent: false,
            lockoutOnFailure: false);

        // Assert
        Assert.That(result.Succeeded, Is.True, "Sign in should succeed with valid credentials");
        Assert.That(result.IsLockedOut, Is.False);
        Assert.That(result.IsNotAllowed, Is.False);
        Assert.That(result.RequiresTwoFactor, Is.False);
    }

    [Test]
    public async Task PasswordSignInAsync_WithInvalidPassword_Fails()
    {
        // Arrange
        await IdentitySeeder.SeedAsync(_serviceProvider);
        var signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        // Act
        var result = await signInManager.PasswordSignInAsync(
            "admin@experimentdemo.com",
            "WrongPassword!",
            isPersistent: false,
            lockoutOnFailure: false);

        // Assert
        Assert.That(result.Succeeded, Is.False, "Sign in should fail with invalid password");
    }

    [Test]
    public async Task PasswordSignInAsync_WithNonexistentUser_Fails()
    {
        // Arrange
        await IdentitySeeder.SeedAsync(_serviceProvider);
        var signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        // Act
        var result = await signInManager.PasswordSignInAsync(
            "nonexistent@example.com",
            "Admin123!",
            isPersistent: false,
            lockoutOnFailure: false);

        // Assert
        Assert.That(result.Succeeded, Is.False, "Sign in should fail for nonexistent user");
    }

    [Test]
    public async Task CheckPasswordAsync_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        await IdentitySeeder.SeedAsync(_serviceProvider);
        var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("admin@experimentdemo.com");

        Assert.That(user, Is.Not.Null);

        // Act
        var isValid = await userManager.CheckPasswordAsync(user!, "Admin123!");

        // Assert
        Assert.That(isValid, Is.True, "Password should be valid");
    }

    [Test]
    public async Task AllSeededUsers_CanSignIn()
    {
        // Arrange
        await IdentitySeeder.SeedAsync(_serviceProvider);
        var signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        var credentials = new[]
        {
            ("admin@experimentdemo.com", "Admin123!"),
            ("experimenter@experimentdemo.com", "Experimenter123!"),
            ("viewer@experimentdemo.com", "Viewer123!"),
            ("analyst@experimentdemo.com", "Analyst123!")
        };

        // Act & Assert
        foreach (var (email, password) in credentials)
        {
            var result = await signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

            Assert.That(result.Succeeded, Is.True, $"Sign in should succeed for {email}");
        }
    }
}

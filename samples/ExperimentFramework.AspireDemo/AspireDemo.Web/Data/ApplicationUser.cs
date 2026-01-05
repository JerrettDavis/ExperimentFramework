using Microsoft.AspNetCore.Identity;

namespace AspireDemo.Web.Data;

/// <summary>
/// Application user model with additional properties for demonstration.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the user's department.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the user can access experiments.
    /// </summary>
    public bool CanAccessExperiments { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the user can modify experiments.
    /// </summary>
    public bool CanModifyExperiments { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the user can manage rollouts.
    /// </summary>
    public bool CanManageRollouts { get; set; } = false;
}

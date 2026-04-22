namespace ExperimentFramework.E2E.Tests.Support;

public record TestUser(string Email, string Password, string Role);

public static class TestUsers
{
    public static readonly TestUser Admin =
        new("admin@experimentdemo.com", "Admin123!", "Admin");

    public static readonly TestUser Experimenter =
        new("experimenter@experimentdemo.com", "Experimenter123!", "Experimenter");

    public static readonly TestUser Viewer =
        new("viewer@experimentdemo.com", "Viewer123!", "Viewer");

    public static readonly TestUser Analyst =
        new("analyst@experimentdemo.com", "Analyst123!", "Analyst");

    public static TestUser GetByRole(string role) => role.ToLowerInvariant() switch
    {
        "admin"        => Admin,
        "experimenter" => Experimenter,
        "viewer"       => Viewer,
        "analyst"      => Analyst,
        _              => throw new ArgumentException($"Unknown role: '{role}'. Valid roles: Admin, Experimenter, Viewer, Analyst.")
    };
}

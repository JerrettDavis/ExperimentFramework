using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.SqlServer.Data;

public class ExperimentDataContextFactory : IDesignTimeDbContextFactory<ExperimentDataContext>
{
    public ExperimentDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExperimentDataContext>();
        
        // Use a design-time connection string for migrations
        optionsBuilder.UseSqlServer("Server=localhost;Database=ExperimentFramework;Trusted_Connection=True;");

        var backplaneOptions = Options.Create(new SqlServerDataBackplaneOptions
        {
            ConnectionString = "Server=localhost;Database=ExperimentFramework;Trusted_Connection=True;",
            Schema = "dbo",
            TableName = "ExperimentEvents"
        });

        return new ExperimentDataContext(optionsBuilder.Options, backplaneOptions);
    }
}

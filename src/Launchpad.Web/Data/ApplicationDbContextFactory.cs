using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Launchpad.Web.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {
    public ApplicationDbContext CreateDbContext(string[] args) {
        DbContextOptionsBuilder<ApplicationDbContext> options = new();
        string basePath = Directory.GetCurrentDirectory();
        string projectPath = Path.Combine(basePath, "src", "Launchpad.Web");
        if (Directory.Exists(projectPath)) {
            basePath = projectPath;
        }

        string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("launchpaddb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=launchpaddb;Username=postgres;Password=postgres";

        options.UseNpgsql(connectionString);
        return new ApplicationDbContext(options.Options);
    }
}

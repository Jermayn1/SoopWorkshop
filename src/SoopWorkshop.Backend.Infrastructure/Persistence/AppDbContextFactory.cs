using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SoopWorkshop.Backend.Infrastructure.Persistence
{
    // Wird ausschliesslich von den EF-Core-Tools benutzt (dotnet ef migrations / database update).
    // Liest denselben Connection-String wie Backend.API: appsettings.json, User Secrets,
    // Umgebungsvariablen - in dieser Reihenfolge, spaetere gewinnen.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // Muss mit der UserSecretsId in SoopWorkshop.Backend.API.csproj uebereinstimmen
        private const string UserSecretsId = "soopworkshop-backend-api";

        public AppDbContext CreateDbContext(string[] args)
        {
            var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SoopWorkshop.Backend.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets(UserSecretsId)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Es ist kein Connection-String gesetzt. Lokal ueber User Secrets setzen:" + Environment.NewLine +
                    "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=soopworkshop;Username=postgres;Password=DEIN_PASSWORT\" --project src/SoopWorkshop.Backend.API");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

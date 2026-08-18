using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoopWorkshop.Backend.Infrastructure.Evaluation;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Startet die echte API im Speicher - dieselbe Program.cs, dieselbe
    /// Pipeline, dieselben Controller.
    /// </summary>
    public sealed class SoopWorkshopFactory(string connectionString) : WebApplicationFactory<Program>
    {
        /// <summary>Das Passwort, mit dem sich Tests anmelden.</summary>
        public const string AdminPassword = "test-passwort";

        /// <summary>Der Ursprung, den die CORS-Regel erlauben soll.</summary>
        public const string AllowedOrigin = "http://localhost:5173";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Pflicht, und zwar aus einem konkreten Grund: in Development laedt
            // Program.cs die .env des Repositorys als LETZTE Konfigurationsquelle
            // (bewusst so, siehe CLAUDE.md Paragraph 3). Sie wuerde die Werte hier
            // ueberschreiben, und die Tests liefen gegen die Entwicklungsdatenbank.
            builder.UseEnvironment("Testing");

            // UseSetting statt ConfigureAppConfiguration: beide Werte werden
            // gebraucht, bevor irgendein Test-Code laufen kann. AddInfrastructure
            // wirft bei leerem Connection-String, AddAdminAuthentication bei
            // fehlendem Passwort - beides absichtlich, damit nichts still ohne
            // Datenbank oder ohne Zugangsschutz startet.
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Admin:Password", AdminPassword);
            builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

            builder.ConfigureTestServices(services =>
            {
                // Der EvaluationWorker setzt beim Start verwaiste Pending- und
                // Running-Abgaben auf Failed. In einem Test, der genau so eine
                // Abgabe angelegt hat, aendert er damit die Datenlage unter der
                // Hand. Ausserdem braeuchte er ein JDK.
                var worker = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(EvaluationWorker));

                if (worker is not null)
                    services.Remove(worker);
            });
        }
    }
}

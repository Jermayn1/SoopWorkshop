using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Infrastructure.Configuration;

namespace SoopWorkshop.Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Wendet ausstehende Migrationen beim Start an.
    /// </summary>
    /// <remarks>
    /// Ein IHostedService und bewusst kein BackgroundService: die Arbeit steht in
    /// StartAsync und haelt damit den Start auf, bis das Schema steht. Ein
    /// BackgroundService gaebe die Ausfuehrung beim ersten await zurueck - der
    /// EvaluationWorker griffe dann auf eine womoeglich noch nicht migrierte
    /// Datenbank zu. Aus demselben Grund wird dieser Dienst VOR ihm registriert.
    ///
    /// Ein Fehlschlag bricht den Start ab, und das ist Absicht: ohne Schema
    /// beantwortet die API jede Anfrage mit einem 500er, ein weiterlaufender
    /// Server taeuschte also Betriebsbereitschaft nur vor. Zusammen mit
    /// "restart: unless-stopped" im Compose-Aufbau versucht der Container es von
    /// selbst erneut.
    /// </remarks>
    public class DatabaseMigrationService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DatabaseOptions _options;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            IServiceScopeFactory scopeFactory,
            IOptions<DatabaseOptions> options,
            ILogger<DatabaseMigrationService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.MigrateOnStartup)
            {
                _logger.LogInformation(
                    "Migrationen beim Start sind abgeschaltet (Database:MigrateOnStartup). " +
                    "Das Schema muss von Hand auf dem Stand gehalten werden.");
                return;
            }

            var versuche = Math.Max(1, _options.MigrationRetries);
            var pause = TimeSpan.FromSeconds(Math.Max(1, _options.MigrationRetryDelaySeconds));

            for (var versuch = 1; versuch <= versuche; versuch++)
            {
                try
                {
                    await MigrateAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (versuch < versuche)
                {
                    // Nicht still: der erste Versuch scheitert im Container-Verbund
                    // regelmaessig, und wer die Meldung nie sieht, sucht beim
                    // naechsten echten Fehler an der falschen Stelle.
                    _logger.LogWarning(
                        ex,
                        "Die Datenbank war beim Versuch {Versuch} von {Versuche} nicht bereit. " +
                        "Erneuter Versuch in {Pause} Sekunden.",
                        versuch,
                        versuche,
                        pause.TotalSeconds);

                    await Task.Delay(pause, cancellationToken);
                }
            }

            // Der letzte Versuch laeuft ausserhalb des catch-Filters und wirft
            // damit weiter - mit der Originalausnahme als Ursache.
            await MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task MigrateAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ausstehend = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (ausstehend.Count == 0)
            {
                _logger.LogInformation("Das Datenbankschema ist auf dem Stand.");
                return;
            }

            _logger.LogInformation(
                "{Anzahl} ausstehende Migration(en) werden angewendet: {Migrationen}",
                ausstehend.Count,
                string.Join(", ", ausstehend));

            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Migrationen angewendet.");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Ein PostgreSQL-Container fuer den gesamten Testlauf.
    /// </summary>
    /// <remarks>
    /// Warum ueberhaupt eine echte Datenbank: EF InMemory kennt weder
    /// Transaktionen noch Cascade-Delete noch Fremdschluesselbedingungen. Genau
    /// die traegt aber der Bestands-Transfer aus Etappe 5.4 - ein Rollback-Test
    /// gegen InMemory waere gruen, ohne irgendetwas zu belegen.
    ///
    /// Kosten: einmal je Testlauf Container starten und migrieren (rund 10 bis
    /// 20 Sekunden), danach nichts mehr. Zwischen den Tests raeumt Respawn auf,
    /// das dauert Millisekunden. Erneut zu migrieren waere um ein Vielfaches
    /// teurer.
    /// </remarks>
    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("soopworkshop_test")
            .WithUsername("soop")
            .WithPassword("soop")
            .Build();

        private Respawner _respawner = null!;
        private NpgsqlConnection _respawnConnection = null!;

        public string ConnectionString { get; private set; } = string.Empty;

        /// <summary>
        /// Die API im Speicher. Liegt hier und nicht in der Testklasse, weil
        /// xUnit je Testmethode eine neue Klasseninstanz baut - ein eigener Host
        /// pro Test hiesse, die halbe Anwendung hundertfach hochzufahren.
        /// </summary>
        public SoopWorkshopFactory Factory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            // Ueber die echten Migrationen, nicht ueber EnsureCreated. Damit
            // prueft jeder Testlauf nebenbei, dass der Migrationsstand
            // durchlaeuft - in Phase 5 musste eine Migration von Hand
            // umgeschrieben werden, und so etwas faellt sonst erst im Betrieb auf.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using (var context = new AppDbContext(options))
            {
                await context.Database.MigrateAsync();
            }

            // Respawn haelt eine eigene Verbindung offen: es liest das Schema
            // einmal ein und kennt danach die Loeschreihenfolge selbst.
            _respawnConnection = new NpgsqlConnection(ConnectionString);
            await _respawnConnection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                // Die Migrationshistorie muss stehen bleiben, sonst haelt EF die
                // Datenbank beim naechsten Zugriff fuer nicht migriert.
                TablesToIgnore = ["__EFMigrationsHistory"]
            });

            Factory = new SoopWorkshopFactory(ConnectionString);
        }

        /// <summary>Setzt alle Tabellen zurueck, ohne erneut zu migrieren.</summary>
        public Task ResetAsync() => _respawner.ResetAsync(_respawnConnection);

        public async Task DisposeAsync()
        {
            // Erst die Anwendung, dann die Datenbank unter ihr.
            await Factory.DisposeAsync();
            await _respawnConnection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Alle Integrationstests teilen sich denselben Container. Ohne diese
    /// Sammlung startet xUnit je Testklasse einen eigenen - bei einem Dutzend
    /// Klassen waeren das Minuten statt Sekunden.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "Postgres";
    }
}

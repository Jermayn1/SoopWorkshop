using System.Net;
using System.Net.Http.Json;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Die Abgaben-Uebersicht aus Etappe 7.4.
    /// </summary>
    public class AdminSubmissionsControllerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task<Guid> GivenAufgabe(string kategorie = "OOP")
        {
            var category = PersistedDataFactory.VollstaendigeKategorie(kategorie);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            return category.Tasks.Single().Id;
        }

        private async Task GivenAbgaben(Guid taskItemId, int anzahl, SubmissionStatus status)
        {
            await WithDbAsync(async db =>
            {
                for (var i = 0; i < anzahl; i++)
                {
                    var abgabe = PersistedDataFactory.Abgabe(taskItemId, status);

                    // Auseinandergezogene Zeitpunkte, damit die Sortierung
                    // ueberhaupt etwas zu sortieren hat. Mit identischen
                    // Zeitstempeln entschiede die Datenbank, und der Test
                    // pruefte nur noch Zufall.
                    abgabe.SubmittedAt = DateTime.UtcNow.AddMinutes(-i);
                    db.Submissions.Add(abgabe);
                }

                await db.SaveChangesAsync();
            });
        }

        [Fact]
        public async Task OhneAnmeldung_Liefert401()
        {
            var response = await CreateClient().GetAsync("/api/admin/submissions");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Eine leere Uebersicht ist kein Fehlschlag. Wuerde sie einer sein,
        // zeigte das Panel am ersten Workshop-Tag eine Fehlermeldung.
        [Fact]
        public async Task OhneAbgaben_LiefertEineLeereSeite()
        {
            var client = await CreateAdminClientAsync();

            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            seite.ShouldNotBeNull();
            seite.Items.ShouldBeEmpty();
            seite.Total.ShouldBe(0);
        }

        [Fact]
        public async Task LiefertAufgabeUndKategorieZurZeile()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 1, SubmissionStatus.Pending);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();

            // Genau das, was ohne die Includes im Repository still leer bliebe -
            // die Uebersicht saehe funktionsfaehig aus und naennte nur nichts.
            zeile.TaskTitle.ShouldBe("Bankkonto");
            zeile.CategoryName.ShouldBe("OOP");
            zeile.TaskItemId.ShouldBe(taskId);
            zeile.Status.ShouldBe(SubmissionStatus.Pending);
        }

        // Null und 0 sind nicht dasselbe: 0 waere eine Aussage ueber die
        // Loesung, null sagt nur "noch nicht bewertet".
        [Fact]
        public async Task OhneAuswertung_BleibtDiePunktzahlLeer()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 1, SubmissionStatus.Running);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();
            zeile.TotalScore.ShouldBeNull();
            zeile.MaxScore.ShouldBeNull();
        }

        [Fact]
        public async Task MitAuswertung_NenntDiePunktzahl()
        {
            var taskId = await GivenAufgabe();

            await WithDbAsync(async db =>
            {
                var abgabe = PersistedDataFactory.Abgabe(taskId);
                db.Submissions.Add(abgabe);
                await db.SaveChangesAsync();

                db.EvaluationResults.Add(PersistedDataFactory.Ergebnis(abgabe.Id));
                await db.SaveChangesAsync();
            });

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();
            zeile.TotalScore.ShouldNotBeNull();
            zeile.MaxScore.ShouldNotBeNull();
        }

        [Fact]
        public async Task NeuesteZuerst()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 5, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            seite!.Items
                .Select(i => i.SubmittedAt)
                .ShouldBeInOrder(SortDirection.Descending);
        }

        // Der Test, der die Gesamtzahl von der Seitengroesse trennt. Wuerde
        // Total die Seite zaehlen statt die Menge, stuende im Panel dauerhaft
        // "1 von 1" und niemand kaeme je auf Seite 2.
        [Fact]
        public async Task Blaettert_UndTotalZaehltDieGanzeMenge()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 7, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();

            var ersteSeite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?skip=0&take=3");
            var zweiteSeite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?skip=3&take=3");

            ersteSeite!.Items.Count.ShouldBe(3);
            ersteSeite.Total.ShouldBe(7);

            zweiteSeite!.Items.Count.ShouldBe(3);
            zweiteSeite.Total.ShouldBe(7);

            // Keine Zeile doppelt: sonst waere die Sortierung nicht stabil.
            ersteSeite.Items.Select(i => i.Id)
                .Intersect(zweiteSeite.Items.Select(i => i.Id))
                .ShouldBeEmpty();
        }

        [Fact]
        public async Task FiltertNachStatus()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 2, SubmissionStatus.Done);
            await GivenAbgaben(taskId, 3, SubmissionStatus.Failed);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?status=Failed");

            seite!.Total.ShouldBe(3);
            seite.Items.ShouldAllBe(i => i.Status == SubmissionStatus.Failed);
        }

        [Fact]
        public async Task FiltertNachAufgabe()
        {
            var ersteAufgabe = await GivenAufgabe("OOP");
            var zweiteAufgabe = await GivenAufgabe("Schleifen");

            await GivenAbgaben(ersteAufgabe, 2, SubmissionStatus.Done);
            await GivenAbgaben(zweiteAufgabe, 4, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                $"/api/admin/submissions?taskItemId={zweiteAufgabe}");

            seite!.Total.ShouldBe(4);
            seite.Items.ShouldAllBe(i => i.TaskItemId == zweiteAufgabe);
        }

        // Eine Seitengrenze, die der Aufrufer selbst bestimmt, ist keine.
        [Fact]
        public async Task UeberzogenesTake_WirdGedeckelt()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 3, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?take=100000");

            seite!.Take.ShouldBe(200);
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using SoopWorkshop.Backend.API.Configuration;
using SoopWorkshop.Shared.DTOs.Auth;
using SoopWorkshop.Shared.DTOs.Auth.Requests;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Der Zugangsschutz aus Etappe 5.0, bisher nur von Hand geprueft.
    /// </summary>
    public class AdminAuthenticationTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        // Die Endpunkte, die geschuetzt sein muessen. Ohne diese Liste faellt eine
        // vergessene [Authorize]-Angabe an einem neuen Controller niemandem auf.
        public static TheoryData<string, string> GeschuetzteEndpunkte => new()
        {
            { "GET", "/api/admin/auth/session" },
            { "GET", "/api/admin/categories" },
            { "GET", "/api/admin/tasks" },
            { "GET", "/api/admin/transfer/export" }
        };

        [Theory]
        [MemberData(nameof(GeschuetzteEndpunkte))]
        public async Task OhneAnmeldung_Liefert401(string methode, string pfad)
        {
            var response = await CreateClient().SendAsync(
                new HttpRequestMessage(new HttpMethod(methode), pfad));

            // 401, nicht 302. Die Cookie-Authentifizierung leitet standardmaessig
            // auf eine Anmeldeseite um, die es in einer API nicht gibt - der fetch
            // im Frontend wuerde ihr folgen und am Ende einen 404 auf
            // /Account/Login melden. OnRedirectToLogin ist deshalb umgebogen.
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.Location.ShouldBeNull();
        }

        [Fact]
        public async Task Anmelden_MitRichtigemPasswort_SetztDasCookie()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = SoopWorkshopFactory.AdminPassword });

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            var cookie = response.Headers
                .GetValues("Set-Cookie")
                .ShouldHaveSingleItem();

            cookie.ShouldContain(AdminAuthenticationExtensions.CookieName);

            // Beides ist der Grund, warum es ueberhaupt ein Cookie ist und kein
            // Token im Speicher der Seite: kein Zugriff aus JavaScript, und die
            // Origin-Grenze zwischen Frontend (5173) und API (5120) verlangt
            // SameSite=None, das wiederum Secure verlangt.
            cookie.ShouldContain("httponly", Case.Insensitive);
            cookie.ShouldContain("secure", Case.Insensitive);
            cookie.ShouldContain("samesite=none", Case.Insensitive);
        }

        [Fact]
        public async Task Anmelden_MitFalschemPasswort_Liefert401UndSetztKeinCookie()
        {
            var response = await CreateClient().PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = "falsch" });

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.Contains("Set-Cookie").ShouldBeFalse();

            (await response.Content.ReadAsStringAsync()).ShouldBe("Das Passwort stimmt nicht.");
        }

        [Theory]
        [MemberData(nameof(GeschuetzteEndpunkte))]
        public async Task NachAnmeldung_SindDieEndpunkteErreichbar(string methode, string pfad)
        {
            var client = await CreateAdminClientAsync();

            var response = await client.SendAsync(
                new HttpRequestMessage(new HttpMethod(methode), pfad));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Session_NachAnmeldung_MeldetAngemeldet()
        {
            var client = await CreateAdminClientAsync();

            var session = await client.GetFromJsonAsync<AdminSessionDto>("/api/admin/auth/session");

            session.ShouldNotBeNull();
            session.IsAuthenticated.ShouldBeTrue();
        }

        [Fact]
        public async Task Abmelden_SchliesstDenZugangWieder()
        {
            var client = await CreateAdminClientAsync();

            (await client.PostAsync("/api/admin/auth/logout", null))
                .StatusCode.ShouldBe(HttpStatusCode.NoContent);

            (await client.GetAsync("/api/admin/auth/session"))
                .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Die oeffentliche Seite darf von all dem nichts merken.
        [Fact]
        public async Task OeffentlicheEndpunkte_BrauchenKeineAnmeldung()
        {
            (await CreateClient().GetAsync("/api/categories"))
                .StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}

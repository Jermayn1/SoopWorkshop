using System.Net;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Frontend (5173) und API (5120) sind verschiedene Ursprünge. Stimmt hier
    /// etwas nicht, blockt der Browser jede Anfrage - und der Fehler sieht nach
    /// einem kaputten Backend aus, obwohl beide Seiten fuer sich laufen.
    /// In Phase 0 waren die Schemata schon einmal vertauscht.
    /// </summary>
    public class CorsTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private static HttpRequestMessage Vorabanfrage(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/categories");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            return request;
        }

        [Fact]
        public async Task Vorabanfrage_VomFrontend_WirdErlaubtUndLaesstDasCookieZu()
        {
            var response = await CreateClient().SendAsync(
                Vorabanfrage(SoopWorkshopFactory.AllowedOrigin));

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            response.Headers.GetValues("Access-Control-Allow-Origin")
                .ShouldHaveSingleItem()
                .ShouldBe(SoopWorkshopFactory.AllowedOrigin);

            // Ohne diesen Kopf schickt der Browser das Anmelde-Cookie nicht mit,
            // und jeder Admin-Endpunkt antwortet mit 401.
            response.Headers.GetValues("Access-Control-Allow-Credentials")
                .ShouldHaveSingleItem()
                .ShouldBe("true");
        }

        // Der Gegentest ist der eigentliche Beleg: waere die Regel auf
        // AllowAnyOrigin gestellt, bestuende der Test oben genauso - nur waere
        // die API dann fuer jede fremde Seite offen.
        [Fact]
        public async Task Vorabanfrage_VonFremdemUrsprung_BekommtKeineFreigabe()
        {
            var response = await CreateClient().SendAsync(
                Vorabanfrage("https://irgendwo-anders.example"));

            response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
        }

        [Fact]
        public async Task EchteAnfrage_VomFrontend_TraegtDenFreigabekopf()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
            request.Headers.Add("Origin", SoopWorkshopFactory.AllowedOrigin);

            var response = await CreateClient().SendAsync(request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("Access-Control-Allow-Origin")
                .ShouldHaveSingleItem()
                .ShouldBe(SoopWorkshopFactory.AllowedOrigin);
        }
    }
}

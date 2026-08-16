using SoopWorkshop.Frontend.Web.Components;
using MudBlazor.Services;
using SoopWorkshop.Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5120";
builder.Services.AddFrontendServices(apiBaseUrl);

// Wird nur beim Vorabrendern gebraucht: dort liest MainLayout die Theme-Wahl aus dem
// Cookie, damit die Seite gleich in der richtigen Farbe erscheint.
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Mudblazor Service
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// NotFoundPage in Routes.razor greift nur bei Navigation innerhalb einer laufenden
// Verbindung. Wird eine unbekannte Adresse direkt aufgerufen oder neu geladen, endet die
// Anfrage im Endpoint-Routing, bevor Blazor ueberhaupt laeuft - ohne diese Zeile kam dann
// eine leere Seite mit Status 404 zurueck.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
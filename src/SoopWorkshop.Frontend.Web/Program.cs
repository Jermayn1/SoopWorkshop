using SoopWorkshop.Frontend.Web.Components;
using MudBlazor.Services;
using SoopWorkshop.Frontend.Services;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Frontend.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5120";
builder.Services.AddFrontendServices();

// BaseAddress für alle HTTP-Clients setzen
builder.Services.AddHttpClient<TaskApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<SubmissionApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<AdminApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));

// Themes registrieren
builder.Services.AddScoped<ThemeService>();

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

// Sonst not found bei submissions etc.
// app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
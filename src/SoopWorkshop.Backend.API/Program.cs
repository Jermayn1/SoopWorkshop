using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.API.Configuration;
using SoopWorkshop.Backend.API.Middleware;
using SoopWorkshop.Backend.Application;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Infrastructure;
using SoopWorkshop.Backend.Infrastructure.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// In der Entwicklung ist die .env im Repository-Wurzelverzeichnis die eine Wahrheit —
// dieselbe Datei, aus der docker-compose die Datenbank aufsetzt. Sie wird zuletzt
// hinzugefuegt und schlaegt damit auch Umgebungsvariablen, damit eine vergessene
// Variable in der Shell nicht still etwas anderes bewirkt als in der Datei steht.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddDotEnv(builder.Environment.ContentRootPath);
}

// Add services to the container.

// Enums gehen als Zeichenkette ueber die Leitung, nicht als Zahl — sonst liest ein
// Frontend ausserhalb von .NET "difficulty": 0 und muss die Bedeutung raten.
// Der Konverter steht an den Enums selbst (SoopWorkshop.Shared/Enums), nicht hier:
// eine Registrierung ueber AddJsonOptions wirkt nur zur Laufzeit, der OpenAPI-Erzeuger
// liest den Typ. Beides getrennt zu pflegen hiesse, zwei Wahrheiten zu haben.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Application und Infrastructure einbinden
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Zugangsschutz fuer api/admin/*. Bricht den Start ab, wenn kein Passwort
// gesetzt ist — siehe AdminAuthenticationExtensions.
builder.Services.AddAdminAuthentication(builder.Configuration);

// CORS (Cross-Origin Resource Sharing), erlaubt es dem Frontend Requests an die API zu senden.
// Die erlaubten Origins stehen in der Konfiguration, damit sie im Betrieb ueber
// Umgebungsvariablen gesetzt werden koennen.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Ohne das schickt der Browser das Anmelde-Cookie nicht mit, weil
            // Frontend und API verschiedene Origins sind. Erlaubt ist die
            // Kombination nur, weil die Origins oben namentlich gelistet sind —
            // zusammen mit AllowAnyOrigin lehnt ASP.NET sie ab, und zwar zu Recht.
            .AllowCredentials();
    });
});

var app = builder.Build();

// Welche Werte wirklich gelten, gehoert in die erste Logzeile und nicht in den
// Stacktrace einer fehlgeschlagenen Abfrage. Das Passwort bleibt aussen vor.
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SoopWorkshop.Start");
var evaluationOptions = app.Services.GetRequiredService<IOptions<EvaluationOptions>>().Value;

startupLogger.LogInformation(
    "Konfiguration: Datenbank {Database}, Auswertung {MaxConcurrency} gleichzeitig, " +
    "Zeitgrenzen {CompileTimeout}s kompilieren / {RunTimeout}s ausfuehren.",
    ConnectionStringSummary.Describe(app.Configuration.GetConnectionString("DefaultConnection")),
    evaluationOptions.MaxConcurrency,
    evaluationOptions.CompileTimeoutSeconds,
    evaluationOptions.RunTimeoutSeconds);

// Exception Middleware einbinden
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Zum testen der Endpunkte und Datenbank befüllen zum test
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// CORS einbinden
app.UseCors("AllowFrontend");

// Reihenfolge ist Pflicht: erst feststellen, wer da ist, dann entscheiden, ob
// er darf. Umgedreht liest UseAuthorization eine noch leere Identitaet und
// weist jeden Aufruf ab, auch den angemeldeten.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Nur fuer die Integrationstests. Top-Level-Statements erzeugen zwar eine
// Program-Klasse, aber eine interne - WebApplicationFactory<Program> braucht
// sie oeffentlich. Zur Laufzeit aendert das nichts.
public partial class Program;

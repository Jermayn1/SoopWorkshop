using SoopWorkshop.Backend.API.Middleware;
using SoopWorkshop.Backend.Application;
using SoopWorkshop.Backend.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Application und Infrastructure einbinden
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
            .AllowAnyMethod();
    });
});

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

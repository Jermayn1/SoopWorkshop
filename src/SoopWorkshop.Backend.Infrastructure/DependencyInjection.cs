using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Backend.Infrastructure.Persistence.Repositories;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Evaluation;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Backend.Infrastructure.Processes;

namespace SoopWorkshop.Backend.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Es ist kein Connection-String gesetzt." + Environment.NewLine +
                    "Lokal: .env im Repository-Wurzelverzeichnis anlegen (Vorlage: .env.example) und" + Environment.NewLine +
                    "POSTGRES_PASSWORD setzen - der Connection-String wird daraus gebaut." + Environment.NewLine +
                    "Im Betrieb ueber die Umgebungsvariable ConnectionStrings__DefaultConnection.");
            }

            services.Configure<EvaluationOptions>(
                configuration.GetSection(EvaluationOptions.SectionName));

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<ITaskCategoryRepository, TaskCategoryRepository>();
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<IEvaluationResultRepository, EvaluationResultRepository>();
            services.AddScoped<ITaskTestRepository, TaskTestRepository>();
            services.AddScoped<ITaskUnitTestFileRepository, TaskUnitTestFileRepository>();
            services.AddScoped<ITaskCategoryWeightRepository, TaskCategoryWeightRepository>();

            services.AddScoped<IProcessRunner, ProcessRunner>();

            // Singleton, weil sich Einreihen (Request-Scope) und Abarbeiten
            // (Hintergrunddienst) dieselbe Warteschlange teilen muessen.
            services.AddSingleton<IEvaluationQueue, EvaluationQueue>();
            services.AddHostedService<EvaluationWorker>();

            // Reihenfolge hier ist egal - der JavaAnalyzer sortiert nach
            // IEvaluationChecker.Order. Eine neue Pruefung wird nur ergaenzt.
            services.AddScoped<IEvaluationChecker, CompilabilityChecker>();
            services.AddScoped<IEvaluationChecker, CharacterSetChecker>();
            services.AddScoped<IEvaluationChecker, NamingConventionChecker>();
            services.AddScoped<IEvaluationChecker, TestCaseChecker>();
            services.AddScoped<IEvaluationChecker, JUnitChecker>();

            services.AddScoped<IJavaAnalyzer, JavaAnalyzer>();

            return services;
        }
    }
}

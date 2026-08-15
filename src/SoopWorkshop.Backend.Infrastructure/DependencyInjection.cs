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
                    "Es ist kein Connection-String gesetzt. Lokal ueber User Secrets setzen:" + Environment.NewLine +
                    "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=soopworkshop;Username=postgres;Password=DEIN_PASSWORT\" --project src/SoopWorkshop.Backend.API" + Environment.NewLine +
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

            services.AddScoped<CharacterSetChecker>();
            services.AddScoped<NamingConventionChecker>();
            services.AddScoped<CompilabilityChecker>();
            services.AddScoped<TestCaseChecker>();
            services.AddScoped<IJavaAnalyzer, JavaAnalyzer>();

            return services;
        }
    }
}

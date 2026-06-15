using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Backend.Infrastructure.Persistence.Repositories;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Evaluation;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;

namespace SoopWorkshop.Backend.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ITaskCategoryRepository, TaskCategoryRepository>();
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<IEvaluationResultRepository, EvaluationResultRepository>();

            services.AddScoped<CharacterSetChecker>();
            services.AddScoped<NamingConventionChecker>();
            services.AddScoped<CompilabilityChecker>();
            services.AddScoped<TestCaseChecker>();
            services.AddScoped<IJavaAnalyzer, JavaAnalyzer>();

            return services;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Backend.Infrastructure.Persistence.Repositories;

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

            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Frontend.Services.StateManagement;

namespace SoopWorkshop.Frontend.Services
{
    public static class DependencyInjection
    {
        // Registriert alle typisierten HTTP-Clients samt BaseAddress.
        // apiBaseUrl kommt aus der Konfiguration des aufrufenden Projekts.
        public static IServiceCollection AddFrontendServices(this IServiceCollection services, string apiBaseUrl)
        {
            var baseAddress = new Uri(apiBaseUrl);

            services.AddHttpClient<TaskApiClient>(client => client.BaseAddress = baseAddress);
            services.AddHttpClient<SubmissionApiClient>(client => client.BaseAddress = baseAddress);
            services.AddHttpClient<AdminApiClient>(client => client.BaseAddress = baseAddress);

            services.AddScoped<SubmissionPollingState>();
            services.AddScoped<ThemeService>();

            return services;
        }
    }
}

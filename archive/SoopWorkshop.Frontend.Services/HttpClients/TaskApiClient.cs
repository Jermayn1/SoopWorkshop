using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Frontend.Services.HttpClients
{
    // Kapselt alle API-Class für Aufgaben/Kategorien der Teilnehmer Sicht
    // Gibt nur Sichtbare Kategorien/Aufgaben zurück
    public class TaskApiClient
    {
        private const string UnreachableError =
            "Der Server ist gerade nicht erreichbar.";

        private readonly HttpClient _httpClient;
        private readonly ILogger<TaskApiClient> _logger;

        public TaskApiClient(HttpClient httpClient, ILogger<TaskApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Gibt alle sichtbaren Kategorien mit ihren sichtbaren Aufgaben zurück.
        // Eine leere Liste heisst "es gibt keine sichtbaren Aufgaben" - das ist etwas
        // anderes als ein Fehlschlag und sah frueher gleich aus.
        public async Task<ApiResult<List<TaskCategoryDto>>> GetVisibleCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/categories");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Kategorien konnten nicht geladen werden, die API antwortete mit {StatusCode}.",
                        (int)response.StatusCode);
                    return ApiResult<List<TaskCategoryDto>>.Failure(UnreachableError);
                }

                var categories = await response.Content.ReadFromJsonAsync<List<TaskCategoryDto>>();
                return ApiResult<List<TaskCategoryDto>>.Success(categories ?? []);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Die API ist beim Laden der Kategorien nicht erreichbar.");
                return ApiResult<List<TaskCategoryDto>>.Failure(UnreachableError);
            }
        }

        // Gibt alle Details einer Aufgabe zurück.
        // Frueher stand hier GetFromJsonAsync, das bei 404 wirft - der "nicht gefunden"-Zweig
        // im Frontend war damit unerreichbar und der Teilnehmer sah eine weisse Seite.
        public async Task<ApiResult<TaskItemDto>> GetTaskByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/tasks/{id}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return ApiResult<TaskItemDto>.NotFound();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Aufgabe {TaskItemId} konnte nicht geladen werden, die API antwortete mit {StatusCode}.",
                        id, (int)response.StatusCode);
                    return ApiResult<TaskItemDto>.Failure(UnreachableError);
                }

                var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();

                return task is null
                    ? ApiResult<TaskItemDto>.NotFound()
                    : ApiResult<TaskItemDto>.Success(task);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Die API ist beim Laden der Aufgabe {TaskItemId} nicht erreichbar.", id);
                return ApiResult<TaskItemDto>.Failure(UnreachableError);
            }
        }
    }
}

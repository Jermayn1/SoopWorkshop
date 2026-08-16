using System.Net.Http.Json;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;


namespace SoopWorkshop.Frontend.Services.HttpClients
{
    // Kapselt alle Admin API-Calls für Kategorien, Aufgaben und Testfälle
    public class AdminApiClient
    {
        private readonly HttpClient _httpClient;

        public AdminApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ────── Kategorien ────────────
        public async Task<List<TaskCategoryDto>> GetAllCategoriesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<TaskCategoryDto>>("api/admin/categories");
            return result ?? [];
        }

        public async Task<TaskCategoryDto?> CreateCategoryAsync(CreateTaskCategoryDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/categories", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskCategoryDto>()
                : null;
        }

        public async Task<TaskCategoryDto?> UpdateCategoryAsync(UpdateTaskCategoryDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/categories/{dto.Id}", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskCategoryDto>()
                : null;
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/admin/categories/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleCategoryVisibilityAsync(Guid id)
        {
            var response = await _httpClient.PatchAsync($"api/admin/categories/{id}/visibility", null);
            return response.IsSuccessStatusCode;
        }


        // ────── Aufgaben ────────────

        public async Task<List<TaskItemDto>> GetAllTasksAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<TaskItemDto>>("api/admin/tasks");
            return result ?? [];
        }

        public async Task<TaskItemDto?> GetTaskByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<TaskItemDto>($"api/admin/tasks/{id}");
        }

        public async Task<TaskItemDto?> CreateTaskAsync(CreateTaskItemDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/tasks", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskItemDto>()
                : null;
        }

        public async Task<TaskItemDto?> UpdateTaskAsync(UpdateTaskItemDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/tasks/{dto.Id}", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskItemDto>()
                : null;
        }

        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/admin/tasks/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleTaskVisibilityAsync(Guid id)
        {
            var response = await _httpClient.PatchAsync($"api/admin/tasks/{id}/visibility", null);
            return response.IsSuccessStatusCode;
        }

        // ────── Testfälle ────────────

        public async Task<List<TaskTestDto>> GetTestsByTaskIdAsync(Guid taskItemId)
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<TaskTestDto>>($"api/admin/tasks/{taskItemId}/tests");
            return result ?? [];
        }

        public async Task<TaskTestDto?> CreateTestAsync(CreateTaskTestDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/admin/tasks/{dto.TaskItemId}/tests", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskTestDto>()
                : null;
        }

        public async Task<TaskTestDto?> UpdateTestAsync(Guid taskItemId, UpdateTaskTestDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/tasks/{taskItemId}/tests/{dto.Id}", dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TaskTestDto>()
                : null;
        }

        public async Task<bool> DeleteTestAsync(Guid taskItemId, Guid testId)
        {
            var response = await _httpClient.DeleteAsync($"api/admin/tasks/{taskItemId}/tests/{testId}");
            return response.IsSuccessStatusCode;
        }
    }
}
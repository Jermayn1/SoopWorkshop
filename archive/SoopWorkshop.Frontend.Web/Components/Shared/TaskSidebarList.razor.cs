using Microsoft.AspNetCore.Components;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Frontend.Web.Components.Shared
{
    public partial class TaskSidebarList : ComponentBase
    {
        [Inject] private TaskApiClient TaskApiClient { get; set; } = default!;

        private List<TaskCategoryDto> _categories = [];
        private bool _isLoading = true;
        private bool _loadFailed;

        protected override Task OnInitializedAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            _isLoading = true;
            _loadFailed = false;

            var result = await TaskApiClient.GetVisibleCategoriesAsync();

            _loadFailed = result.Failed;
            _categories = result.Value ?? [];

            _isLoading = false;
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            StateHasChanged();
        }

        // Die API sortiert bereits. Hier wird es trotzdem angewendet, damit die Anzeige
        // nicht davon abhaengt, dass niemand die Reihenfolge unterwegs verliert -
        // dieselbe Ueberlegung wie in SubmissionResult.
        private IEnumerable<TaskCategoryDto> SortedCategories =>
            _categories.OrderBy(category => category.Order);

        private static IEnumerable<TaskItemDto> SortedTasks(TaskCategoryDto category) =>
            category.Tasks.OrderBy(task => task.Order);
    }
}

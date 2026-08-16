using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Frontend.Web.Components.Pages.Tasks
{
    public partial class TaskDetail : ComponentBase
    {
        [Parameter] public Guid Id { get; set; }

        [Inject] private TaskApiClient TaskApiClient { get; set; } = default!;
        [Inject] private SubmissionApiClient SubmissionApiClient { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private TaskItemDto? _task;
        private bool _isLoading = true;
        private bool _isSubmitting = false;

        // Getrennt vom Fehler des Uploads: "die Aufgabe laesst sich nicht laden" und
        // "die Abgabe wurde abgelehnt" stehen an verschiedenen Stellen der Seite.
        private string? _loadError;
        private string? _errorMessage;

        private List<IBrowserFile> _selectedFiles = [];

        protected override Task OnParametersSetAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            _isLoading = true;
            _errorMessage = null;
            _loadError = null;
            _selectedFiles = [];

            var result = await TaskApiClient.GetTaskByIdAsync(Id);

            _task = result.Value;
            _loadError = result.ErrorMessage;
            _isLoading = false;
        }

        private async Task RetryLoadAsync()
        {
            await LoadAsync();
            StateHasChanged();
        }

        private void OnFilesChanged(IReadOnlyList<IBrowserFile>? files)
        {
            _selectedFiles = files?.ToList() ?? [];
            _errorMessage = null;
        }

        private async Task SubmitAsync()
        {
            if (_selectedFiles.Count == 0)
                return;

            _isSubmitting = true;
            _errorMessage = null;

            var upload = await SubmissionApiClient.SubmitAsync(Id, _selectedFiles);

            // Die Begruendung kommt vom Server - "nur .java erlaubt" hilft weiter,
            // "Fehler beim Hochladen" nicht.
            if (upload.Value is null)
            {
                _errorMessage = upload.ErrorMessage;
                _isSubmitting = false;
                return;
            }

            _isSubmitting = false;
            Navigation.NavigateTo($"/result/{upload.Value.Id}");
        }

        private Color GetDifficultyColor() => _task?.Difficulty switch
        {
            Difficulty.Easy => Color.Success,
            Difficulty.Medium => Color.Warning,
            Difficulty.Hard => Color.Error,
            _ => Color.Default
        };
    }
}

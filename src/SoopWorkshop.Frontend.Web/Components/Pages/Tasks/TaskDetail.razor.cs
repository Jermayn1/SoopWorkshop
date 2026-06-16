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
        private string? _errorMessage;
        private List<IBrowserFile> _selectedFiles = [];

        protected override async Task OnParametersSetAsync()
        {
            _isLoading = true;
            _errorMessage = null;
            _selectedFiles = [];
            _task = await TaskApiClient.GetTaskByIdAsync(Id);
            _isLoading = false;
        }

        private void OnFilesChanged(InputFileChangeEventArgs e)
        {
            _selectedFiles = e.GetMultipleFiles(10).ToList();
            _errorMessage = null;
        }

        private async Task SubmitAsync()
        {
            if (_selectedFiles.Count == 0)
                return;

            _isSubmitting = true;
            _errorMessage = null;

            var submission = await SubmissionApiClient.SubmitAsync(Id, _selectedFiles);

            if (submission is null)
            {
                _errorMessage = "Fehler beim Hochladen. Bitte versuche es erneut.";
                _isSubmitting = false;
                return;
            }

            Navigation.NavigateTo($"/result/{submission.Id}");
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
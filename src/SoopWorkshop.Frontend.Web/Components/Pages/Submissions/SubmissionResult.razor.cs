using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Frontend.Services.StateManagment;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Frontend.Web.Components.Pages.Submissions;

public partial class SubmissionResult : ComponentBase, IAsyncDisposable
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private SubmissionApiClient SubmissionApiClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private EvaluationResultDto? _result;
    private bool _isPolling = true;
    private SubmissionPollingState? _pollingState;

    protected override async Task OnInitializedAsync()
    {
        _pollingState = new SubmissionPollingState(SubmissionApiClient);
        _pollingState.OnResultReceived += OnResultReceived;
        _pollingState.OnError += OnError;
        _pollingState.StartPolling(Id);
        await Task.CompletedTask;
    }

    private void OnResultReceived(EvaluationResultDto result)
    {
        _result = result;
        _isPolling = false;
        InvokeAsync(StateHasChanged);
    }

    private void OnError(string error)
    {
        _isPolling = false;
        InvokeAsync(StateHasChanged);
    }

    private void GoBack() => Navigation.NavigateTo("/");

    private double GetScorePercentage() =>
        _result is null || _result.MaxScore == 0
            ? 0
            : (double)_result.TotalScore / _result.MaxScore * 100;

    private Color GetScoreColor() => GetScorePercentage() switch
    {
        >= 80 => Color.Success,
        >= 50 => Color.Warning,
        _ => Color.Error
    };

    private string GetScoreLabel() => GetScorePercentage() switch
    {
        100 => "Hervorragende Arbeit!",
        >= 80 => "Sehr gut!",
        >= 50 => "Gut gemacht!",
        >= 30 => "Weiter ueben!",
        _ => "Nicht bestanden"
    };

    private static double GetCategoryPercentage(CategoryResultDto category) =>
        category.MaxPoints == 0 ? 0 : (double)category.Points / category.MaxPoints * 100;

    private static string GetCategoryName(EvaluationCategory category) => category switch
    {
        EvaluationCategory.CharacterSet => EvaluationCategoryNames.CharacterSet,
        EvaluationCategory.NamingConventions => EvaluationCategoryNames.NamingConventions,
        EvaluationCategory.Compilability => EvaluationCategoryNames.Compilability,
        EvaluationCategory.CleanCode => EvaluationCategoryNames.CleanCode,
        EvaluationCategory.TestCases => EvaluationCategoryNames.TestCases,
        _ => category.ToString()
    };

    public async ValueTask DisposeAsync()
    {
        if (_pollingState is not null)
        {
            _pollingState.OnResultReceived -= OnResultReceived;
            _pollingState.OnError -= OnError;
            await _pollingState.DisposeAsync();
        }
    }
}
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Frontend.Services.StateManagement;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Frontend.Web.Components.Pages.Submissions;

public partial class SubmissionResult : ComponentBase, IAsyncDisposable
{
    [Parameter] public Guid Id { get; set; }

    // Der Dienst ist als Scoped registriert und gehoert damit dem Circuit —
    // deshalb injiziert und nicht selbst erzeugt.
    [Inject] private SubmissionPollingState PollingState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private EvaluationResultDto? _result;
    private bool _isPolling = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        PollingState.OnResultReceived += OnResultReceived;
        PollingState.OnError += OnError;
        PollingState.StartPolling(Id);
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
        _errorMessage = error;
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
        EvaluationCategory.UnitTests => EvaluationCategoryNames.UnitTests,
        EvaluationCategory.Functionality => EvaluationCategoryNames.Functionality,
        _ => category.ToString()
    };

    // Die API liefert bereits sortiert. Hier wird es trotzdem angewendet, damit
    // die Anzeige nicht davon abhaengt, dass niemand die Reihenfolge unterwegs
    // verliert — Sortierung ist billig, eine wechselnde Anzeige verwirrt.
    private static IEnumerable<CategoryResultDto> SortedCategories(EvaluationResultDto result) =>
        result.CategoryResults.OrderBy(category => EvaluationCategoryOrder.Of(category.Category));

    private static IEnumerable<TestCaseResultDto> SortedTestCases(CategoryResultDto category) =>
        category.TestCaseResults.OrderBy(testCase => testCase.Order);

    // Manche Teilpruefungen sind reine Ja/Nein-Aussagen ("Kein Umlaut gefunden")
    // und haben nichts zu zeigen. Dann bleibt der Detailblock ganz weg, statt
    // leere Beschriftungen zu hinterlassen.
    // Der abschliessende Zeilenumbruch von println haengt an fast jeder Ausgabe
    // und erzeugt sonst eine leere Zeile unter dem Wert. Er kann nie die Ursache
    // eines Fehlschlags sein, weil der Vergleich beide Seiten ohnehin trimmt.
    // Leerzeichen und Umbrueche *innerhalb* der Ausgabe bleiben unangetastet -
    // die sind oft genau der Grund.
    private static string ForDisplay(string value) => value.TrimEnd('\r', '\n');

    private static bool HasDetails(TestCaseResultDto testCase) =>
        !string.IsNullOrWhiteSpace(testCase.Input)
        || !string.IsNullOrWhiteSpace(testCase.ExpectedOutput)
        || !string.IsNullOrWhiteSpace(testCase.ActualOutput);

    // Nur abmelden und stoppen: den Lebenszyklus des Dienstes verwaltet die DI.
    public async ValueTask DisposeAsync()
    {
        PollingState.OnResultReceived -= OnResultReceived;
        PollingState.OnError -= OnError;
        PollingState.StopPolling();

        await ValueTask.CompletedTask;
    }
}
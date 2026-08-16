using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Submissions.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepository,
    IEvaluationResultRepository evaluationResultRepository,
    ITaskItemRepository taskItemRepository,
    IEvaluationQueue evaluationQueue,
    ILogger<SubmissionService> logger) : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository = submissionRepository;
    private readonly IEvaluationResultRepository _evaluationResultRepository = evaluationResultRepository;
    private readonly ITaskItemRepository _taskItemRepository = taskItemRepository;
    private readonly IEvaluationQueue _evaluationQueue = evaluationQueue;
    private readonly ILogger<SubmissionService> _logger = logger;

    public async Task<Result<SubmissionDto>> CreateAsync(
        Guid taskItemId,
        List<(string FileName, string Content)> files,
        CancellationToken cancellationToken)
    {
        // Ohne diese Pruefung schlaegt erst die Fremdschluesselbedingung zu und
        // der Teilnehmer bekommt einen 500er statt einer Erklaerung.
        if (!await _taskItemRepository.ExistsAsync(taskItemId, cancellationToken))
            return Result<SubmissionDto>.Fail("Aufgabe nicht gefunden.");

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskItemId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Pending,
            Files = files.Select(f => new SubmissionFile
            {
                Id = Guid.NewGuid(),
                FileName = f.FileName,
                Content = f.Content
            }).ToList()
        };

        await _submissionRepository.AddAsync(submission);

        // Die Auswertung uebernimmt der EvaluationWorker. Bleibt eine Abgabe hier
        // haengen, faengt das Aufraeumen beim naechsten Start sie ab.
        await _evaluationQueue.EnqueueAsync(submission.Id, cancellationToken);

        _logger.LogInformation(
            "Abgabe {SubmissionId} zu Aufgabe {TaskItemId} eingereiht ({FileCount} Datei(en)).",
            submission.Id,
            taskItemId,
            files.Count);

        return Result<SubmissionDto>.Ok(MapToDto(submission));
    }

    public async Task<Result<EvaluationResultDto>> GetResultAsync(Guid submissionId)
    {
        var submission = await _submissionRepository.GetByIdAsync(submissionId);
        if (submission is null)
            return Result<EvaluationResultDto>.Fail("Einreichung nicht gefunden.");

        if (submission.Status == SubmissionStatus.Pending || submission.Status == SubmissionStatus.Running)
            return Result<EvaluationResultDto>.Fail($"Auswertung läuft noch. Status: {submission.Status}");

        var result = await _evaluationResultRepository.GetBySubmissionIdAsync(submissionId);
        if (result is null)
            return Result<EvaluationResultDto>.Fail("Kein Ergebnis gefunden.");

        return Result<EvaluationResultDto>.Ok(MapResultToDto(result));
    }

    public async Task<Result<SubmissionStatusDto>> GetStatusAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await _submissionRepository.GetSummaryByIdAsync(submissionId, cancellationToken);
        if (submission is null)
            return Result<SubmissionStatusDto>.Fail("Einreichung nicht gefunden.");

        return Result<SubmissionStatusDto>.Ok(new SubmissionStatusDto
        {
            Id = submission.Id,
            TaskItemId = submission.TaskItemId,
            Status = submission.Status,
            SubmittedAt = submission.SubmittedAt,
            ErrorMessage = submission.ErrorMessage
        });
    }

    private static SubmissionDto MapToDto(Submission submission) => new()
    {
        Id = submission.Id,
        TaskItemId = submission.TaskItemId,
        SubmittedAt = submission.SubmittedAt,
        Status = submission.Status
    };

    private static EvaluationResultDto MapResultToDto(EvaluationResult result) => new()
    {
        Id = result.Id,
        SubmissionId = result.SubmissionId,
        TotalScore = result.TotalScore,
        MaxScore = result.MaxScore,
        // Sortiert ausliefern: die Datenbank gibt die Zeilen sonst in beliebiger
        // Reihenfolge zurueck und die Ergebnisseite sieht bei jedem Aufruf anders aus.
        CategoryResults = result.CategoryResults
            .OrderBy(c => EvaluationCategoryOrder.Of(c.Category))
            .Select(c => new CategoryResultDto
            {
                Id = c.Id,
                Category = c.Category,
                Passed = c.Passed,
                Points = c.Points,
                MaxPoints = c.MaxPoints,
                ErrorTip = c.ErrorTip,
                TestCaseResults = c.TestCaseResults
                    .OrderBy(t => t.Order)
                    .Select(t => new TestCaseResultDto
                    {
                        Id = t.Id,
                        Description = t.Description,
                        Input = t.Input,
                        ExpectedOutput = t.ExpectedOutput,
                        ActualOutput = t.ActualOutput,
                        Passed = t.Passed,
                        Order = t.Order
                    }).ToList()
            }).ToList()
    };
}
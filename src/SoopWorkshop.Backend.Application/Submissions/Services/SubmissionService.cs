using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Submissions.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepository,
    IEvaluationResultRepository evaluationResultRepository,
    IServiceScopeFactory scopeFactory) : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository = submissionRepository;
    private readonly IEvaluationResultRepository _evaluationResultRepository = evaluationResultRepository;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public async Task<Result<SubmissionDto>> CreateAsync(Guid taskItemId, List<(string FileName, string Content)> files)
    {
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

        // Eigener Scope, da dieser Task ausserhalb des HTTP-Request-Lebenszyklus laeuft
        // und sonst auf einen bereits entsorgten DbContext zugreifen wuerde.
        var submissionId = submission.Id;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var evaluationService = scope.ServiceProvider.GetRequiredService<IEvaluationService>();
            await evaluationService.EvaluateAsync(submissionId, CancellationToken.None);
        });

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
        CategoryResults = result.CategoryResults.Select(c => new CategoryResultDto
        {
            Id = c.Id,
            Category = c.Category,
            Passed = c.Passed,
            Points = c.Points,
            MaxPoints = c.MaxPoints,
            ErrorTip = c.ErrorTip,
            TestCaseResults = c.TestCaseResults.Select(t => new TestCaseResultDto
            {
                Id = t.Id,
                Description = t.Description,
                ExpectedOutput = t.ExpectedOutput,
                ActualOutput = t.ActualOutput,
                Passed = t.Passed
            }).ToList()
        }).ToList()
    };
}
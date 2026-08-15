using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;

namespace SoopWorkshop.Backend.Application.Submissions.Interfaces
{
    public interface ISubmissionService
    {
        Task<Result<SubmissionDto>> CreateAsync(
            Guid taskItemId,
            List<(string FileName, string Content)> files,
            CancellationToken cancellationToken);
        Task<Result<EvaluationResultDto>> GetResultAsync(Guid submissionId);

        // Schlaegt nur fehl, wenn es die Abgabe nicht gibt — "laeuft noch" und
        // "fehlgeschlagen" sind gueltige Antworten, keine Fehler.
        Task<Result<SubmissionStatusDto>> GetStatusAsync(Guid submissionId, CancellationToken cancellationToken);
    }
}
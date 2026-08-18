using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

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

        // Uebersicht fuer das Panel. Eine leere Seite ist kein Fehlschlag:
        // "es gibt noch keine Abgaben" ist eine gueltige Auskunft.
        Task<Result<SubmissionPageDto>> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken);
    }
}
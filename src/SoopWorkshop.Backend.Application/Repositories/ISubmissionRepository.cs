using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(Guid id);
        Task<List<Submission>> GetByTaskIdAsync(Guid taskId);
        Task AddAsync(Submission submission);
        Task UpdateAsync(Submission submission);

        // Ohne Dateien, Aufgabe und Testfaelle — fuer Statusabfragen, die nur
        // Status und Fehlermeldung brauchen.
        Task<Submission?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken);

        // Fuer das Aufraeumen verwaister Auswertungen beim Start.
        Task<List<Guid>> GetIdsByStatusAsync(IReadOnlyList<SubmissionStatus> statuses, CancellationToken cancellationToken);

        // Aendert gezielt nur Status und Fehlermeldung. UpdateAsync wuerde den
        // kompletten Graphen inklusive Dateien neu schreiben.
        Task UpdateStatusAsync(Guid id, SubmissionStatus status, string errorMessage, CancellationToken cancellationToken);
    }
}

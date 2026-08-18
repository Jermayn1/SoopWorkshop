using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(Guid id);
        Task<List<Submission>> GetByTaskIdAsync(Guid taskId);

        // Seitenweise Liste fuer die Uebersicht im Panel, neueste zuerst.
        // Laedt Aufgabe, Kategorie und Auswertung mit — aber NICHT die Dateien:
        // die Liste zeigt nur deren Anzahl, und ihr Inhalt waere bei hunderten
        // Zeilen die teuerste Spalte der Abfrage.
        Task<(List<Submission> Items, int Total)> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken);
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

using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly AppDbContext _context;

        public SubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Submission?> GetByIdAsync(Guid id)
        {
            // Die Auswertung liest alles ueber submission.Task - was hier fehlt,
            // sieht der JavaAnalyzer als "nicht vorhanden" und bewertet entsprechend.
            return await _context.Submissions
                .Include(s => s.Files)
                .Include(s => s.Task)
                    .ThenInclude(t => t.Tests)
                .Include(s => s.Task)
                    .ThenInclude(t => t.CategoryWeights)
                .Include(s => s.Task)
                    .ThenInclude(t => t.UnitTestFiles)
                .Include(s => s.Task)
                    .ThenInclude(t => t.ExpectedTypes)
                        .ThenInclude(type => type.Methods)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Submission>> GetByTaskIdAsync(Guid taskId)
        {
            return await _context.Submissions
                .Where(s => s.TaskItemId == taskId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<(List<Submission> Items, int Total)> GetPageAsync(
            Guid? taskItemId,
            SubmissionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var abfrage = _context.Submissions.AsNoTracking().AsQueryable();

            if (taskItemId is not null)
                abfrage = abfrage.Where(s => s.TaskItemId == taskItemId);

            if (status is not null)
                abfrage = abfrage.Where(s => s.Status == status);

            // Vor dem Blaettern zaehlen, sonst zaehlt man die Seite statt der
            // Menge - und die Seitennavigation im Panel zeigte immer "1 von 1".
            var gesamt = await abfrage.CountAsync(cancellationToken);

            var items = await abfrage
                // Aufgabe UND deren Kategorie: die Zeile nennt beide. Fehlte das
                // Include, stuende dort ein leerer Name statt einer Fehlermeldung
                // - dieselbe stille Fehlerquelle wie bei GetByIdAsync.
                .Include(s => s.Task)
                    .ThenInclude(t => t.Category)
                .Include(s => s.EvaluationResult)
                .OrderByDescending(s => s.SubmittedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, gesamt);
        }

        public async Task AddAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }

        public async Task<Submission?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<List<Guid>> GetIdsByStatusAsync(
            IReadOnlyList<SubmissionStatus> statuses,
            CancellationToken cancellationToken)
        {
            return await _context.Submissions
                .AsNoTracking()
                .Where(s => statuses.Contains(s.Status))
                .OrderBy(s => s.SubmittedAt)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateStatusAsync(
            Guid id,
            SubmissionStatus status,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            await _context.Submissions
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(s => s.Status, status)
                        .SetProperty(s => s.ErrorMessage, errorMessage),
                    cancellationToken);
        }
    }
}
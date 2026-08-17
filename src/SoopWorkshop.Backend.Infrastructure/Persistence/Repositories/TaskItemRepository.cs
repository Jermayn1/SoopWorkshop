using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskItemRepository : ITaskItemRepository
    {
        private readonly AppDbContext _context;

        public TaskItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .Include(t => t.Tests)
                .Include(t => t.UnitTestFiles)
                .Include(t => t.ExpectedMethods)
                // CategoryWeights fehlte hier. Was nicht mitgeladen wird, sieht
                // die Auswertung als "nicht vorhanden" und bewertet entsprechend -
                // die stillste denkbare Fehlerquelle.
                .Include(t => t.CategoryWeights)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.TaskItems.AnyAsync(t => t.Id == id, cancellationToken);
        }

        public async Task AddAsync(TaskItem item)
        {
            _context.TaskItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskItem item)
        {
            // Kommt die Aufgabe aus GetByIdAsync, ist sie schon verfolgt - dann
            // erledigt die Aenderungsverfolgung alles, auch das Anlegen neuer und
            // das Loeschen entfernter Kindzeilen.
            //
            // Update() waere dann nicht falsch, aber verschwenderisch: es markiert
            // den GANZEN geladenen Graphen als geaendert und schreibt bei jedem
            // Speichern auch jeden Testfall, jede JUnit-Datei und jedes Gewicht
            // neu, obwohl sich davon nichts angefasst wurde.
            if (_context.Entry(item).State == EntityState.Detached)
                _context.TaskItems.Update(item);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.TaskItems.FindAsync(id);
            if (item is not null)
            {
                _context.TaskItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
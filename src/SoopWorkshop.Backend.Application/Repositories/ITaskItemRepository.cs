using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ITaskItemRepository
    {
        Task<List<TaskItem>> GetAllAsync();
        Task<TaskItem?> GetByIdAsync(Guid id);

        // Reine Existenzpruefung — GetByIdAsync laedt Hints und Testfaelle mit,
        // die dafuer niemand braucht.
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(TaskItem item);
        Task UpdateAsync(TaskItem item);
        Task DeleteAsync(Guid id);
    }
}
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories.Interfaces
{
    public interface ITaskCategoryRepository
    {
        Task<List<TaskCategory>> GetAllAsync();
        Task<List<TaskCategory>> GetAllVisibleAsync();
        Task<TaskCategory?> GetByIdAsync(Guid id);
        Task AddAsync(TaskCategory category);
        Task UpdateAsync(TaskCategory category);
        Task DeleteAsync(Guid id);
    }
}
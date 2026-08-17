using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ITaskTestRepository
    {
        Task<List<TaskTest>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<TaskTest?> GetByIdAsync(Guid id);
        Task AddAsync(TaskTest test);
        Task UpdateAsync(TaskTest test);
        Task DeleteAsync(Guid id);

        // Ersetzt alle Testfaelle einer Aufgabe in einem Zug, wie es
        // ITaskUnitTestFileRepository fuer die JUnit-Dateien tut.
        Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskTest> tests);
    }
}
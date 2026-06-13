using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories.Interfaces
{
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(Guid id);
        Task<List<Submission>> GetByTaskIdAsync(Guid taskId);
        Task AddAsync(Submission submission);
        Task UpdateAsync(Submission submission);
    }
}

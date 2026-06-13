using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Admin;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskItemService
    {
        Task<Result<List<TaskItemDto>>> GetAllAsync();
        Task<Result<List<TaskItemDto>>> GetVisibleByCategoryAsync(Guid categoryId);
        Task<Result<TaskItemDto>> GetByIdAsync(Guid id);
        Task<Result<TaskItemDto>> CreateAsync(CreateTaskItemDto dto);
        Task<Result<TaskItemDto>> UpdateAsync(UpdateTaskItemDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
        Task<Result<bool>> ToggleVisibilityAsync(Guid id);
    }
}
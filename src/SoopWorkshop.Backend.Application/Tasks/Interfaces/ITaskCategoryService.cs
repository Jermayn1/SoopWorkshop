using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Admin;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskCategoryService
    {
        Task<Result<List<TaskCategoryDto>>> GetAllAsync();
        Task<Result<List<TaskCategoryDto>>> GetAllVisibleAsync();
        Task<Result<TaskCategoryDto>> GetByIdAsync(Guid id);
        Task<Result<TaskCategoryDto>> CreateAsync(CreateTaskCategoryDto dto);
        Task<Result<TaskCategoryDto>> UpdateAsync(UpdateTaskCategoryDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
        Task<Result<bool>> ToggleVisibilityAsync(Guid id);
    }
}
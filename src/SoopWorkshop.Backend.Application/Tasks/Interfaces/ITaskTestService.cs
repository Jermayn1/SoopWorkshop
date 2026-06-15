using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Admin;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskTestService
    {
        Task<Result<List<UpdateTaskTestDto>>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<Result<UpdateTaskTestDto>> CreateAsync(CreateTaskTestDto dto);
        Task<Result<UpdateTaskTestDto>> UpdateAsync(UpdateTaskTestDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Admin;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tasks/{taskItemId:guid}/tests")]
    public class AdminTaskTestsController : ControllerBase
    {
        private readonly ITaskTestService _taskTestService;

        public AdminTaskTestsController(ITaskTestService taskTestService)
        {
            _taskTestService = taskTestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByTaskItem(Guid taskItemId)
        {
            var result = await _taskTestService.GetByTaskItemIdAsync(taskItemId);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid taskItemId, [FromBody] CreateTaskTestDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskTestService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid taskItemId, Guid id, [FromBody] UpdateTaskTestDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskTestService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid taskItemId, Guid id)
        {
            var result = await _taskTestService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }
    }
}
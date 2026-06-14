using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Admin;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tasks")]
    public class AdminTasksController : ControllerBase
    {
        private readonly ITaskItemService _taskItemService;

        public AdminTasksController(ITaskItemService taskItemService)
        {
            _taskItemService = taskItemService;
        }

        // Gibt alle Aufgaben zurück
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _taskItemService.GetAllAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _taskItemService.GetByIdAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskItemDto dto)
        {
            var result = await _taskItemService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskItemDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskItemService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _taskItemService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }

        // Toggelt die Sichtbarkeit einer Aufgabe
        [HttpPatch("{id:guid}/visibility")]
        public async Task<IActionResult> ToggleVisibility(Guid id)
        {
            var result = await _taskItemService.ToggleVisibilityAsync(id);

            return result.IsSuccess
                ? Ok(new { isVisible = result.Value })
                : NotFound(result.ErrorMessage);
        }
    }
}
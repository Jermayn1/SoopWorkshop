using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;

namespace SoopWorkshop.Backend.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskCategoryService _categoryService;
        private readonly ITaskItemService _taskItemService;

        public TasksController(ITaskCategoryService categoryService, ITaskItemService taskItemService)
        {
            _categoryService = categoryService;
            _taskItemService = taskItemService;
        }

        /// Gibt alle sichtbaren Kategorien und ihre sichtbaren Aufgaben zurück
        [HttpGet("categories")]
        public async Task<IActionResult> GetVisibleCategories()
        {
            var result = await _categoryService.GetAllVisibleAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        /// Gibt die Details einer Aufgabe zurück (Aufgabenstellung, Tipps, etc.)
        [HttpGet("tasks/{id:guid}")]
        public async Task<IActionResult> GetTaskById(Guid id)
        {
            var result = await _taskItemService.GetByIdAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // Aufgabenspezifische Gewichte der Bewertungskategorien. Ohne Eintrag gelten
    // die Standardgewichte aus Evaluation:CategoryWeights.
    [ApiController]
    [Route("api/admin/tasks/{taskItemId:guid}/weights")]
    public class AdminTaskCategoryWeightsController : ControllerBase
    {
        private readonly ITaskCategoryWeightService _service;

        public AdminTaskCategoryWeightsController(ITaskCategoryWeightService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetByTaskItem(Guid taskItemId)
        {
            var result = await _service.GetByTaskItemIdAsync(taskItemId);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Ersetzt alle Gewichte der Aufgabe. Eine leere Liste stellt die
        // Standardgewichte wieder her.
        [HttpPut]
        public async Task<IActionResult> SaveAll(Guid taskItemId, [FromBody] SaveTaskCategoryWeightsDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.SaveAllAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }
    }
}

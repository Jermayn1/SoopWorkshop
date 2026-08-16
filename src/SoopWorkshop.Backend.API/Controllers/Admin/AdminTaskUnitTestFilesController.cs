using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // JUnit-Dateien einer Aufgabe. Bewusst bequemer geschnitten als die aelteren
    // Admin-Endpunkte: die Sichtbarkeit kommt beim Anlegen mit, und PUT auf die
    // Sammlung speichert alle Dateien in einem Aufruf.
    [ApiController]
    [Route("api/admin/tasks/{taskItemId:guid}/unittests")]
    public class AdminTaskUnitTestFilesController : ControllerBase
    {
        private readonly ITaskUnitTestFileService _service;

        public AdminTaskUnitTestFilesController(ITaskUnitTestFileService service)
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

        [HttpPost]
        public async Task<IActionResult> Create(Guid taskItemId, [FromBody] CreateTaskUnitTestFileDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Ersetzt alle Dateien der Aufgabe. Was nicht im Body steht, wird geloescht.
        [HttpPut]
        public async Task<IActionResult> SaveAll(Guid taskItemId, [FromBody] SaveTaskUnitTestFilesDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.SaveAllAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid taskItemId, Guid id, [FromBody] UpdateTaskUnitTestFileDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid taskItemId, Guid id)
        {
            var result = await _service.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }
    }
}

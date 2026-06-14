using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Admin;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ITaskCategoryService _categoryService;

        public AdminCategoriesController(ITaskCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // Gibt alle Kategorien zurück
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _categoryService.GetAllAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCategoryDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _categoryService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _categoryService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }

        // Toggelt die Sichtbarkeit einer Kategorie
        [HttpPatch("{id:guid}/visibility")]
        public async Task<IActionResult> ToggleVisibility(Guid id)
        {
            var result = await _categoryService.ToggleVisibilityAsync(id);

            return result.IsSuccess
                ? Ok(new { isVisible = result.Value })
                : NotFound(result.ErrorMessage);
        }
    }
}
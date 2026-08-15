using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskCategoryService(
        ITaskCategoryRepository repository,
        ILogger<TaskCategoryService> logger) : ITaskCategoryService
    {
        private readonly ITaskCategoryRepository _repository = repository;
        private readonly ILogger<TaskCategoryService> _logger = logger;

        public async Task<Result<List<TaskCategoryDto>>> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<TaskCategoryDto>>.Ok(dtos);
        }

        public async Task<Result<List<TaskCategoryDto>>> GetAllVisibleAsync()
        {
            var categories = await _repository.GetAllVisibleAsync();
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<TaskCategoryDto>>.Ok(dtos);
        }

        public async Task<Result<TaskCategoryDto>> GetByIdAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category is null)
                return Result<TaskCategoryDto>.Fail("Kategorie nicht gefunden.");

            return Result<TaskCategoryDto>.Ok(MapToDto(category));
        }

        public async Task<Result<TaskCategoryDto>> CreateAsync(CreateTaskCategoryDto dto)
        {
            var category = new TaskCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Order = dto.Order,
                IsVisible = false
            };

            await _repository.AddAsync(category);

            _logger.LogInformation("Kategorie {CategoryId} '{Name}' angelegt.", category.Id, category.Name);

            return Result<TaskCategoryDto>.Ok(MapToDto(category));
        }

        public async Task<Result<TaskCategoryDto>> UpdateAsync(UpdateTaskCategoryDto dto)
        {
            var category = await _repository.GetByIdAsync(dto.Id);
            if (category is null)
                return Result<TaskCategoryDto>.Fail("Kategorie nicht gefunden.");

            category.Name = dto.Name;
            category.Order = dto.Order;
            category.IsVisible = dto.IsVisible;

            await _repository.UpdateAsync(category);

            _logger.LogInformation("Kategorie {CategoryId} geaendert.", category.Id);

            return Result<TaskCategoryDto>.Ok(MapToDto(category));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category is null)
                return Result<bool>.Fail("Kategorie nicht gefunden.");

            await _repository.DeleteAsync(id);

            // Loeschen entfernt per Kaskade auch alle Aufgaben darunter.
            _logger.LogInformation("Kategorie {CategoryId} '{Name}' geloescht.", id, category.Name);

            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> ToggleVisibilityAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category is null)
                return Result<bool>.Fail("Kategorie nicht gefunden.");

            category.IsVisible = !category.IsVisible;
            await _repository.UpdateAsync(category);

            _logger.LogInformation(
                "Kategorie {CategoryId} ist jetzt {Visibility}.",
                id,
                category.IsVisible ? "sichtbar" : "verborgen");

            return Result<bool>.Ok(category.IsVisible);
        }

        private static TaskCategoryDto MapToDto(TaskCategory category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Order = category.Order,
            IsVisible = category.IsVisible,
            Tasks = category.Tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                TaskCategoryId = t.TaskCategoryId,
                Title = t.Title,
                Description = t.Description,
                Difficulty = t.Difficulty,
                Order = t.Order,
                IsVisible = t.IsVisible,
                EvaluationMode = t.EvaluationMode
            }).ToList()
        };
    }
}

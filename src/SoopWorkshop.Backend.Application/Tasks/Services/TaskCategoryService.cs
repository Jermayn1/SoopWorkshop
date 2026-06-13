using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories.Interfaces;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Admin;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskCategoryService(ITaskCategoryRepository repository) : ITaskCategoryService
    {
        private readonly ITaskCategoryRepository _repository = repository;

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
            return Result<TaskCategoryDto>.Ok(MapToDto(category));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category is null)
                return Result<bool>.Fail("Kategorie nicht gefunden.");

            await _repository.DeleteAsync(id);
            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> ToggleVisibilityAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category is null)
                return Result<bool>.Fail("Kategorie nicht gefunden.");

            category.IsVisible = !category.IsVisible;
            await _repository.UpdateAsync(category);
            return Result<bool>.Ok(category.IsVisible);
        }

        private static TaskCategoryDto MapToDto(TaskCategory category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Order = category.Order,
            IsVisible = category.IsVisible
        };
    }
}

using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories.Interfaces;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Admin;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskItemService(ITaskItemRepository repository) : ITaskItemService
    {
        private readonly ITaskItemRepository _repository = repository;

        public async Task<Result<List<TaskItemDto>>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            var dtos = items.Select(MapToDto).ToList();
            return Result<List<TaskItemDto>>.Ok(dtos);
        }

        public async Task<Result<List<TaskItemDto>>> GetVisibleByCategoryAsync(Guid categoryId)
        {
            var items = await _repository.GetVisibleByCategoryAsync(categoryId);
            var dtos = items.Select(MapToDto).ToList();
            return Result<List<TaskItemDto>>.Ok(dtos);
        }

        public async Task<Result<TaskItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<TaskItemDto>.Fail("Aufgabe nicht gefunden.");

            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<TaskItemDto>> CreateAsync(CreateTaskItemDto dto)
        {
            var item = new TaskItem
            {
                Id = Guid.NewGuid(),
                TaskCategoryId = dto.TaskCategoryId,
                Title = dto.Title,
                Description = dto.Description,
                Difficulty = dto.Difficulty,
                Order = dto.Order,
                IsVisible = false,
                Hints = dto.Hints.Select((content, index) => new TaskHint
                {
                    Id = Guid.NewGuid(),
                    Content = content,
                    Order = index + 1
                }).ToList()
            };

            await _repository.AddAsync(item);
            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<TaskItemDto>> UpdateAsync(UpdateTaskItemDto dto)
        {
            var item = await _repository.GetByIdAsync(dto.Id);
            if (item is null)
                return Result<TaskItemDto>.Fail("Aufgabe nicht gefunden.");

            item.Title = dto.Title;
            item.Description = dto.Description;
            item.Difficulty = dto.Difficulty;
            item.Order = dto.Order;
            item.IsVisible = dto.IsVisible;

            await _repository.UpdateAsync(item);
            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.Fail("Aufgabe nicht gefunden.");

            await _repository.DeleteAsync(id);
            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> ToggleVisibilityAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.Fail("Aufgabe nicht gefunden.");

            item.IsVisible = !item.IsVisible;
            await _repository.UpdateAsync(item);
            return Result<bool>.Ok(item.IsVisible);
        }

        private static TaskItemDto MapToDto(TaskItem item) => new()
        {
            Id = item.Id,
            TaskCategoryId = item.TaskCategoryId,
            Title = item.Title,
            Description = item.Description,
            Difficulty = item.Difficulty,
            Order = item.Order,
            IsVisible = item.IsVisible,
            Hints = item.Hints
                .OrderBy(h => h.Order)
                .Select(h => new TaskHintDto
                {
                    Id = h.Id,
                    TaskItemId = h.TaskItemId,
                    Content = h.Content,
                    Order = h.Order
                }).ToList()
        };
    }
}

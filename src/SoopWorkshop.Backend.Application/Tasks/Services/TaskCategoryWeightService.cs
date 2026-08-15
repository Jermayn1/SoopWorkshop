using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskCategoryWeightService : ITaskCategoryWeightService
    {
        private readonly ITaskCategoryWeightRepository _repository;
        private readonly ILogger<TaskCategoryWeightService> _logger;

        public TaskCategoryWeightService(
            ITaskCategoryWeightRepository repository,
            ILogger<TaskCategoryWeightService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<List<TaskCategoryWeightDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var weights = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<TaskCategoryWeightDto>>.Ok(weights.Select(MapToDto).ToList());
        }

        public async Task<Result<List<TaskCategoryWeightDto>>> SaveAllAsync(SaveTaskCategoryWeightsDto dto)
        {
            var duplicate = dto.Weights
                .GroupBy(weight => weight.Category)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                return Result<List<TaskCategoryWeightDto>>.Fail(
                    $"Fuer die Kategorie {duplicate.Key} sind mehrere Gewichte angegeben.");

            var invalid = dto.Weights.FirstOrDefault(weight => weight.Weight <= 0);
            if (invalid is not null)
                return Result<List<TaskCategoryWeightDto>>.Fail(
                    $"Das Gewicht fuer {invalid.Category} muss groesser als 0 sein.");

            var weights = dto.Weights
                .Select(entry => new TaskCategoryWeight
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = dto.TaskItemId,
                    Category = entry.Category,
                    Weight = entry.Weight
                })
                .ToList();

            await _repository.ReplaceForTaskItemAsync(dto.TaskItemId, weights);

            _logger.LogInformation(
                "Gewichte von Aufgabe {TaskItemId} ersetzt: {Count} Ueberschreibung(en).",
                dto.TaskItemId, weights.Count);

            return Result<List<TaskCategoryWeightDto>>.Ok(weights.Select(MapToDto).ToList());
        }

        private static TaskCategoryWeightDto MapToDto(TaskCategoryWeight weight) => new()
        {
            Id = weight.Id,
            TaskItemId = weight.TaskItemId,
            Category = weight.Category,
            Weight = weight.Weight
        };
    }
}

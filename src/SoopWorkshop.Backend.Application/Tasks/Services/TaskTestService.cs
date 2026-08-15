using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskTestService : ITaskTestService
    {
        private readonly ITaskTestRepository _repository;
        private readonly ILogger<TaskTestService> _logger;

        public TaskTestService(ITaskTestRepository repository, ILogger<TaskTestService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<List<TaskTestDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var tests = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<TaskTestDto>>.Ok(tests.Select(MapToDto).ToList());
        }

        public async Task<Result<TaskTestDto>> CreateAsync(CreateTaskTestDto dto)
        {
            var test = new TaskTest
            {
                Id = Guid.NewGuid(),
                TaskItemId = dto.TaskItemId,
                Input = dto.Input,
                ExpectedOutput = dto.ExpectedOutput,
                Description = dto.Description,
                Order = dto.Order
            };

            await _repository.AddAsync(test);

            _logger.LogInformation(
                "Testfall {TaskTestId} zu Aufgabe {TaskItemId} angelegt.", test.Id, test.TaskItemId);

            return Result<TaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<TaskTestDto>> UpdateAsync(UpdateTaskTestDto dto)
        {
            var test = await _repository.GetByIdAsync(dto.Id);
            if (test is null)
                return Result<TaskTestDto>.Fail("Testfall nicht gefunden.");

            test.Input = dto.Input;
            test.ExpectedOutput = dto.ExpectedOutput;
            test.Description = dto.Description;
            test.Order = dto.Order;

            await _repository.UpdateAsync(test);

            _logger.LogInformation("Testfall {TaskTestId} geaendert.", test.Id);

            return Result<TaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var test = await _repository.GetByIdAsync(id);
            if (test is null)
                return Result<bool>.Fail("Testfall nicht gefunden.");

            await _repository.DeleteAsync(id);

            _logger.LogInformation("Testfall {TaskTestId} geloescht.", id);

            return Result<bool>.Ok(true);
        }

        private static TaskTestDto MapToDto(TaskTest test) => new()
        {
            Id = test.Id,
            TaskItemId = test.TaskItemId,
            Input = test.Input,
            ExpectedOutput = test.ExpectedOutput,
            Description = test.Description,
            Order = test.Order
        };
    }
}

using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Admin;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskTestService : ITaskTestService
    {
        private readonly ITaskTestRepository _repository;

        public TaskTestService(ITaskTestRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<List<UpdateTaskTestDto>>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            var tests = await _repository.GetByTaskItemIdAsync(taskItemId);
            return Result<List<UpdateTaskTestDto>>.Ok(tests.Select(MapToDto).ToList());
        }

        public async Task<Result<UpdateTaskTestDto>> CreateAsync(CreateTaskTestDto dto)
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
            return Result<UpdateTaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<UpdateTaskTestDto>> UpdateAsync(UpdateTaskTestDto dto)
        {
            var test = await _repository.GetByIdAsync(dto.Id);
            if (test is null)
                return Result<UpdateTaskTestDto>.Fail("Testfall nicht gefunden.");

            test.Input = dto.Input;
            test.ExpectedOutput = dto.ExpectedOutput;
            test.Description = dto.Description;
            test.Order = dto.Order;

            await _repository.UpdateAsync(test);
            return Result<UpdateTaskTestDto>.Ok(MapToDto(test));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var test = await _repository.GetByIdAsync(id);
            if (test is null)
                return Result<bool>.Fail("Testfall nicht gefunden.");

            await _repository.DeleteAsync(id);
            return Result<bool>.Ok(true);
        }

        private static UpdateTaskTestDto MapToDto(TaskTest test) => new()
        {
            Id = test.Id,
            Input = test.Input,
            ExpectedOutput = test.ExpectedOutput,
            Description = test.Description,
            Order = test.Order
        };
    }
}
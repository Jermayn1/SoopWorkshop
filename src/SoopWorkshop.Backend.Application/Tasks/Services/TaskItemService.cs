using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Tasks.Services
{
    public class TaskItemService(
        ITaskItemRepository repository,
        ILogger<TaskItemService> logger) : ITaskItemService
    {
        private readonly ITaskItemRepository _repository = repository;
        private readonly ILogger<TaskItemService> _logger = logger;

        public async Task<Result<List<TaskItemDto>>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
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
                IsVisible = dto.IsVisible,
                EvaluationMode = dto.EvaluationMode,
                ExpectedSignatures = dto.ExpectedSignatures,
                Hints = dto.Hints.Select((content, index) => new TaskHint
                {
                    Id = Guid.NewGuid(),
                    Content = content,
                    Order = index + 1
                }).ToList()
            };

            await _repository.AddAsync(item);

            _logger.LogInformation("Aufgabe {TaskItemId} '{Title}' angelegt.", item.Id, item.Title);

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
            item.EvaluationMode = dto.EvaluationMode;
            item.ExpectedSignatures = dto.ExpectedSignatures;


            item.Hints.Clear();
            foreach (var (content, index) in dto.Hints.Select((content, index) => (content, index)))
            {
                item.Hints.Add(new TaskHint
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = item.Id,
                    Content = content,
                    Order = index + 1
                });
            }

            await _repository.UpdateAsync(item);

            _logger.LogInformation("Aufgabe {TaskItemId} geaendert.", item.Id);

            return Result<TaskItemDto>.Ok(MapToDto(item));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.Fail("Aufgabe nicht gefunden.");

            await _repository.DeleteAsync(id);

            _logger.LogInformation("Aufgabe {TaskItemId} '{Title}' geloescht.", id, item.Title);

            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> ToggleVisibilityAsync(Guid id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return Result<bool>.Fail("Aufgabe nicht gefunden.");

            if (!item.IsVisible)
            {
                var problem = DescribeMissingTestData(item);
                if (problem is not null)
                    return Result<bool>.Fail(problem);
            }

            item.IsVisible = !item.IsVisible;
            await _repository.UpdateAsync(item);

            _logger.LogInformation(
                "Aufgabe {TaskItemId} ist jetzt {Visibility}.",
                id,
                item.IsVisible ? "sichtbar" : "verborgen");

            return Result<bool>.Ok(item.IsVisible);
        }

        // Eine sichtbare Aufgabe muss auch pruefbar sein. Geprueft wird erst beim
        // Sichtbarschalten und nicht beim Anlegen: beim Anlegen gibt es die
        // Testfaelle noch gar nicht, die Aufgabe entsteht ja erst.
        //
        // Ohne diese Pruefung wird eine Aufgabe mit vergessener Testdatei still
        // milder bewertet - die Kategorie faellt weg und ihr Gewicht verteilt sich.
        private static string? DescribeMissingTestData(TaskItem item)
        {
            var needsConsoleTests = item.EvaluationMode is EvaluationMode.ConsoleOnly or EvaluationMode.Both;
            var needsUnitTests = item.EvaluationMode is EvaluationMode.UnitTestOnly or EvaluationMode.Both;

            if (needsConsoleTests && item.Tests.Count == 0)
                return $"Die Aufgabe ist auf '{item.EvaluationMode}' gestellt, hat aber keinen Konsolen-Testfall. " +
                       "Lege zuerst mindestens einen Testfall an oder stelle den Modus um.";

            if (needsUnitTests && item.UnitTestFiles.Count == 0)
                return $"Die Aufgabe ist auf '{item.EvaluationMode}' gestellt, hat aber keine JUnit-Datei. " +
                       "Hinterlege zuerst mindestens eine Testdatei oder stelle den Modus um.";

            return null;
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
            EvaluationMode = item.EvaluationMode,
            ExpectedSignatures = item.ExpectedSignatures,
            Hints = item.Hints
                .OrderBy(h => h.Order)
                .Select(h => new TaskHintDto
                {
                    Id = h.Id,
                    TaskItemId = h.TaskItemId,
                    Content = h.Content,
                    Order = h.Order
                }).ToList(),

            // Bewusst gefiltert: nicht freigeschaltete Testdateien verlassen den
            // Admin-Bereich nicht, sonst schreibt man auf den Test hin.
            VisibleUnitTestFiles = item.UnitTestFiles
                .Where(file => file.IsVisibleToParticipant)
                .OrderBy(file => file.Order)
                .Select(file => new TaskUnitTestFileDto
                {
                    Id = file.Id,
                    TaskItemId = file.TaskItemId,
                    FileName = file.FileName,
                    Content = file.Content,
                    Order = file.Order,
                    IsVisibleToParticipant = file.IsVisibleToParticipant
                }).ToList()
        };
    }
}

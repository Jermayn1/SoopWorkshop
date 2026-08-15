using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Submissions.Services;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Tests.Unit.Application.Submissions
{
    public class SubmissionServiceTests
    {
        private readonly ISubmissionRepository _submissionRepository = Substitute.For<ISubmissionRepository>();
        private readonly IEvaluationResultRepository _evaluationResultRepository = Substitute.For<IEvaluationResultRepository>();
        private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
        private readonly IEvaluationQueue _evaluationQueue = Substitute.For<IEvaluationQueue>();

        private static readonly List<(string FileName, string Content)> Files =
            [("Main.java", "public class Main {}")];

        private SubmissionService CreateService() =>
            new(_submissionRepository,
                _evaluationResultRepository,
                _taskItemRepository,
                _evaluationQueue,
                NullLogger<SubmissionService>.Instance);

        private Guid GivenExistingTask()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(true);
            return taskItemId;
        }

        [Fact]
        public async Task CreateAsync_AufgabeExistiert_SpeichertUndReihtGenauEinmalEin()
        {
            var taskItemId = GivenExistingTask();

            var result = await CreateService().CreateAsync(taskItemId, Files, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value!.TaskItemId.ShouldBe(taskItemId);

            await _submissionRepository.Received(1).AddAsync(Arg.Is<Submission>(s => s.Files.Count == 1));
            await _evaluationQueue.Received(1).EnqueueAsync(result.Value.Id, Arg.Any<CancellationToken>());
        }

        // Frueher schlug erst die Fremdschluesselbedingung zu — der Teilnehmer
        // bekam einen 500er statt einer Erklaerung.
        [Fact]
        public async Task CreateAsync_AufgabeExistiertNicht_LiefertFehlerUndReihtNichtsEin()
        {
            var taskItemId = Guid.NewGuid();
            _taskItemRepository.ExistsAsync(taskItemId, Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().CreateAsync(taskItemId, Files, CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Aufgabe");

            await _submissionRepository.DidNotReceive().AddAsync(Arg.Any<Submission>());
            await _evaluationQueue.DidNotReceive().EnqueueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_MehrereDateien_UebernimmtAlleInDieAbgabe()
        {
            var taskItemId = GivenExistingTask();
            List<(string FileName, string Content)> files =
                [("Main.java", "class Main {}"), ("Helfer.java", "class Helfer {}")];

            await CreateService().CreateAsync(taskItemId, files, CancellationToken.None);

            await _submissionRepository.Received(1).AddAsync(Arg.Is<Submission>(s =>
                s.Files.Count == 2 && s.Files.Any(f => f.FileName == "Helfer.java")));
        }
    }
}

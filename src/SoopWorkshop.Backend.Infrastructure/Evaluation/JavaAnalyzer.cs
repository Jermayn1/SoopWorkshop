using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation
{
    public class JavaAnalyzer : IJavaAnalyzer
    {
        private readonly CharacterSetChecker _characterSetChecker;
        private readonly NamingConventionChecker _namingConventionChecker;
        private readonly CompilabilityChecker _compilabilityChecker;
        private readonly TestCaseChecker _testCaseChecker;
        private readonly ILogger<JavaAnalyzer> _logger;

        public JavaAnalyzer(
            CharacterSetChecker characterSetChecker,
            NamingConventionChecker namingConventionChecker,
            CompilabilityChecker compilabilityChecker,
            TestCaseChecker testCaseChecker,
            ILogger<JavaAnalyzer> logger)
        {
            _characterSetChecker = characterSetChecker;
            _namingConventionChecker = namingConventionChecker;
            _compilabilityChecker = compilabilityChecker;
            _testCaseChecker = testCaseChecker;
            _logger = logger;
        }

        public async Task<EvaluationResult> AnalyzeAsync(
            Submission submission,
            List<TaskTest> expectedTests,
            CancellationToken cancellationToken)
        {
            var files = submission.Files.ToList();

            // Das Arbeitsverzeichnis gehoert dem Analyzer, damit das finally unten es
            // auch dann loescht, wenn ein Checker eine Exception wirft.
            var workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop", Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDirectory);

            try
            {
                var characterSetResult = _characterSetChecker.Check(files);
                var namingConventionResult = _namingConventionChecker.Check(files);
                var (compilabilityResult, compilation) = await _compilabilityChecker.CheckAsync(files, workingDirectory, cancellationToken);
                var testCaseResult = await _testCaseChecker.CheckAsync(compilation, expectedTests, cancellationToken);

                var categoryResults = new List<CategoryResult>
                {
                    characterSetResult,
                    namingConventionResult,
                    compilabilityResult,
                    testCaseResult
                };

                var evaluationResult = new EvaluationResult
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    TotalScore = categoryResults.Sum(c => c.Points),
                    MaxScore = categoryResults.Sum(c => c.MaxPoints),
                    CategoryResults = categoryResults
                };

                foreach (var categoryResult in categoryResults)
                {
                    categoryResult.EvaluationResultId = evaluationResult.Id;
                }

                return evaluationResult;
            }
            finally
            {
                CleanupWorkingDirectory(workingDirectory);
            }
        }

        // Löscht das temporäre Verzeichnis mit dem kompilierten Dateien damit der Speicher nicht unnötig voll wird
        private void CleanupWorkingDirectory(string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
                return;

            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Aufräumfehler sollen die Auswertung nicht fehlschlagen lassen — stumm
                // bleiben duerfen sie trotzdem nicht, sonst laeuft die Platte unbemerkt voll.
                _logger.LogWarning(exception, "Arbeitsverzeichnis {WorkingDirectory} konnte nicht geloescht werden.", workingDirectory);
            }
        }
    }
}

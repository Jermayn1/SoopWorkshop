using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Models;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Führt das kompilierte Programm für jeden Testfall aus und vergleicht es mit dem erwareteten Ergebnis
    public class TestCaseChecker
    {
        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;

        public TestCaseChecker(IProcessRunner processRunner, IOptions<EvaluationOptions> options)
        {
            _processRunner = processRunner;
            _options = options.Value;
        }

        public async Task<CategoryResult> CheckAsync(
            CompilationResult compilation,
            List<TaskTest> tests,
            CancellationToken cancellationToken)
        {
            var result = new CategoryResult
            {
                Id = Guid.NewGuid(),
                Category = EvaluationCategory.TestCases,
                MaxPoints = EvaluationCategoryPoints.TestCases
            };

            if (tests.Count == 0)
            {
                result.Passed = true;
                result.Points = EvaluationCategoryPoints.TestCases;
                return result;
            }

            if (!compilation.Success || compilation.MainClassName is null)
            {
                result.ErrorTip = "Da der Code nicht kompiliert, konnten keine Testfaelle ausgefuehrt werden.";

                foreach (var test in tests)
                {
                    result.TestCaseResults.Add(new TestCaseResult
                    {
                        Id = Guid.NewGuid(),
                        Description = test.Description,
                        ExpectedOutput = test.ExpectedOutput,
                        ActualOutput = string.Empty,
                        Passed = false
                    });
                }

                return result;
            }

            foreach (var test in tests)
            {
                var actualOutput = await RunProgramAsync(compilation, test.Input, cancellationToken);
                var passed = NormalizeOutput(actualOutput) == NormalizeOutput(test.ExpectedOutput);

                result.TestCaseResults.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = test.Description,
                    ExpectedOutput = test.ExpectedOutput,
                    ActualOutput = actualOutput,
                    Passed = passed
                });
            }

            var allPassed = result.TestCaseResults.All(t => t.Passed);
            var pointsPerTest = EvaluationCategoryPoints.TestCases / tests.Count;
            var passedCount = result.TestCaseResults.Count(t => t.Passed);

            // Rundungsverlust (z.B. 65 / 3 = 21) bekommt man bei bestandenen Tests trotzdem die volle Punktzahl
            result.Points = allPassed ? EvaluationCategoryPoints.TestCases : passedCount * pointsPerTest;
            result.Passed = allPassed;

            if (!allPassed)
                result.ErrorTip = "Pruefe deine Ausgabe genau gegen die erwartete Ausgabe - achte auf Gross-/Kleinschreibung, Leerzeichen und Zeilenumbrueche.";

            return result;
        }

        // Entfernt führende und abschließende Leerzeichen und vereinheitlicht Zeilenumbrueche,
        // damit kleine Formatierungsunterschiede nicht zu Fehlern führen.
        private static string NormalizeOutput(string output) =>
            output.Replace("\r\n", "\n").Trim();

        private async Task<string> RunProgramAsync(
            CompilationResult compilation,
            string input,
            CancellationToken cancellationToken)
        {
            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "java",
                    [compilation.MainClassName!],
                    compilation.WorkingDirectory,
                    input,
                    TimeSpan.FromSeconds(_options.RunTimeoutSeconds)),
                cancellationToken);

            if (process.ExecutableNotFound)
                return "'java' wurde nicht gefunden. Ist das JDK installiert und im PATH?";

            if (process.TimedOut)
                return $"Zeitueberschreitung: Das Programm hat laenger als {_options.RunTimeoutSeconds} Sekunden gebraucht. " +
                       "Pruefe, ob eine Schleife nie endet oder auf eine Eingabe gewartet wird, die es nicht gibt.";

            // Bricht das Programm ab, ohne etwas auszugeben, ist der Stacktrace die
            // einzige Information, die dem Teilnehmer weiterhilft.
            if (string.IsNullOrWhiteSpace(process.StandardOutput) && !string.IsNullOrWhiteSpace(process.StandardError))
                return process.StandardError;

            return process.StandardOutput;
        }
    }
}

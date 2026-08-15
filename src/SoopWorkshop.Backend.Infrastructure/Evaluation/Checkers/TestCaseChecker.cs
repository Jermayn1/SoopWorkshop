using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Führt das kompilierte Programm für jeden Konsolen-Testfall aus und
    // vergleicht die Ausgabe mit der erwarteten.
    public class TestCaseChecker : IEvaluationChecker
    {
        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;

        public TestCaseChecker(IProcessRunner processRunner, IOptions<EvaluationOptions> options)
        {
            _processRunner = processRunner;
            _options = options.Value;
        }

        public EvaluationCategory Category => EvaluationCategory.TestCases;

        public int Order => EvaluationCheckerOrder.TestCases;

        // Ohne hinterlegte Testfaelle gibt es nichts zu pruefen - dann faellt die
        // Kategorie aus der Wertung und ihr Gewicht verteilt sich auf die uebrigen.
        // Frueher gab es hier stattdessen die volle Punktzahl geschenkt.
        public bool IsApplicable(EvaluationContext context) => context.Task.Tests.Count > 0;

        public async Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var tests = context.Task.Tests.OrderBy(test => test.Order).ToList();
            var compilation = context.Compilation;

            // Kompiliert die Abgabe nicht, gelten alle Testfaelle als nicht
            // bestanden. Die Kategorie faellt bewusst nicht weg - sonst wuerde
            // ihr Gewicht umverteilt und kaputter Code besser bewertet.
            if (compilation is null || !compilation.Success || compilation.MainClassName is null)
            {
                var failed = tests.Select(test => new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = test.Description,
                    ExpectedOutput = test.ExpectedOutput,
                    ActualOutput = string.Empty,
                    Passed = false
                }).ToArray();

                return CheckerOutcome.WithTip(
                    "Da der Code nicht kompiliert, konnten keine Testfaelle ausgefuehrt werden.",
                    failed);
            }

            var results = new List<TestCaseResult>();

            foreach (var test in tests)
            {
                var actualOutput = await RunProgramAsync(compilation, test.Input, cancellationToken);

                results.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = test.Description,
                    ExpectedOutput = test.ExpectedOutput,
                    ActualOutput = actualOutput,
                    Passed = NormalizeOutput(actualOutput) == NormalizeOutput(test.ExpectedOutput)
                });
            }

            return results.All(result => result.Passed)
                ? CheckerOutcome.Of([.. results])
                : CheckerOutcome.WithTip(
                    "Pruefe deine Ausgabe genau gegen die erwartete Ausgabe - achte auf Gross-/Kleinschreibung, Leerzeichen und Zeilenumbrueche.",
                    [.. results]);
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
                    // Ohne diese beiden Angaben schreibt die JVM unter Windows in der
                    // Codepage des Systems (Cp1252) — Umlaute in der Programmausgabe
                    // kaemen dann zerlegt beim Teilnehmer an.
                    ["-Dstdout.encoding=UTF-8", "-Dstderr.encoding=UTF-8", compilation.MainClassName!],
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

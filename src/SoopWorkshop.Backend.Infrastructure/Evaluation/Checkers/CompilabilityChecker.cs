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
    public class CompilabilityChecker
    {
        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;

        public CompilabilityChecker(IProcessRunner processRunner, IOptions<EvaluationOptions> options)
        {
            _processRunner = processRunner;
            _options = options.Value;
        }

        // Das Arbeitsverzeichnis wird vom JavaAnalyzer angelegt und uebergeben, damit
        // dieser es auch dann aufraeumen kann, wenn hier etwas schiefgeht.
        public async Task<(CategoryResult Result, CompilationResult Compilation)> CheckAsync(
            List<SubmissionFile> files,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            var filePaths = new List<string>();

            foreach (var file in files)
            {
                // Zweite Verteidigungslinie hinter der Upload-Pruefung: nur der reine
                // Dateiname darf ins Arbeitsverzeichnis, niemals ein Pfad.
                var filePath = Path.Combine(workingDirectory, Path.GetFileName(file.FileName));
                await File.WriteAllTextAsync(filePath, file.Content, cancellationToken);
                filePaths.Add(filePath);
            }

            // Die Dateien werden als UTF-8 geschrieben; ohne diese Angabe wuerde javac
            // unter Windows mit der Plattform-Codepage lesen und Umlaute zerlegen.
            var arguments = new List<string> { "-encoding", "UTF-8" };
            arguments.AddRange(filePaths);

            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "javac",
                    arguments,
                    workingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.CompileTimeoutSeconds)),
                cancellationToken);

            var success = process.Success;
            var errorOutput = success ? string.Empty : DescribeFailure(process);
            var mainClassName = success ? FindMainClassName(files) : null;

            var result = new CategoryResult
            {
                Id = Guid.NewGuid(),
                Category = EvaluationCategory.Compilability,
                MaxPoints = EvaluationCategoryPoints.Compilability,
                Points = success ? EvaluationCategoryPoints.Compilability : 0,
                Passed = success,
                ErrorTip = success
                    ? string.Empty
                    : "Der Code kompiliert nicht fehlerfrei. Pruefe die Fehlermeldung des Compilers auf Tippfehler oder fehlende Importe."
            };

            result.TestCaseResults.Add(new TestCaseResult
            {
                Id = Guid.NewGuid(),
                Description = "Code kompiliert fehlerfrei",
                Passed = success,
                ActualOutput = errorOutput
            });

            var compilation = new CompilationResult
            {
                Success = success,
                WorkingDirectory = workingDirectory,
                ErrorOutput = errorOutput,
                MainClassName = mainClassName
            };

            return (result, compilation);
        }

        // Uebersetzt das Prozessergebnis in eine Meldung, die dem Teilnehmer sagt,
        // was schiefgelaufen ist — ein leerer Text waere hier wertlos.
        private string DescribeFailure(ProcessResult process)
        {
            if (process.ExecutableNotFound)
                return "'javac' wurde nicht gefunden. Ist das JDK installiert und im PATH?";

            if (process.TimedOut)
                return $"Zeitueberschreitung beim Kompilieren (Grenze: {_options.CompileTimeoutSeconds} Sekunden).";

            return string.IsNullOrWhiteSpace(process.StandardError)
                ? process.StandardOutput
                : process.StandardError;
        }

        // Sucht die Datei mit "public static void main" und gibt den dazugehoerigen
        // Klassennamen zurueck. In Java muss der Dateiname mit dem Klassennamen uebereinstimmen.
        private static string? FindMainClassName(List<SubmissionFile> files)
        {
            var mainFile = files.FirstOrDefault(f => f.Content.Contains("public static void main"));
            return mainFile is null ? null : Path.GetFileNameWithoutExtension(mainFile.FileName);
        }
    }
}

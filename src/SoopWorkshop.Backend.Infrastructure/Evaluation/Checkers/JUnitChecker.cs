using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Junit;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Kompiliert die hinterlegten JUnit-Dateien gegen die Abgabe, fuehrt sie ueber
    // den JUnit-Console-Launcher aus und liest das Ergebnis aus dem XML-Report.
    public class JUnitChecker : IEvaluationChecker
    {
        private const string ReportsDirectoryName = "junit-reports";

        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;
        private readonly ILogger<JUnitChecker> _logger;

        public JUnitChecker(
            IProcessRunner processRunner,
            IOptions<EvaluationOptions> options,
            ILogger<JUnitChecker> logger)
        {
            _processRunner = processRunner;
            _options = options.Value;
            _logger = logger;
        }

        // Dieselbe Kategorie wie die Konsolen-Testfaelle: beide pruefen, ob das
        // Programm die Aufgabe erfuellt, nur auf unterschiedlichem Weg.
        public EvaluationCategory Category => EvaluationCategory.Functionality;

        public int Order => EvaluationCheckerOrder.UnitTests;

        // Der Modus entscheidet, nicht das Vorhandensein von Dateien: eine Aufgabe
        // mit vergessener Testdatei soll auffallen und nicht stillschweigend
        // milder bewertet werden.
        public bool IsApplicable(EvaluationContext context) =>
            context.Task.EvaluationMode is EvaluationMode.UnitTestOnly or EvaluationMode.Both;

        public async Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var testFiles = context.Task.UnitTestFiles.OrderBy(file => file.Order).ToList();

            if (testFiles.Count == 0)
                throw new InvalidOperationException(
                    $"Aufgabe {context.Task.Id} ist auf {context.Task.EvaluationMode} gestellt, " +
                    "hat aber keine JUnit-Datei hinterlegt.");

            var jarPath = ResolveJarPath();
            if (!File.Exists(jarPath))
                throw new InvalidOperationException(
                    $"Das JUnit-JAR wurde unter '{jarPath}' nicht gefunden. " +
                    "Erwartet wird es unter Evaluation:JUnitJarPath.");

            // Kompiliert die Abgabe nicht, gibt es nichts auszufuehren. Die
            // Kategorie faellt trotzdem nicht weg, sonst wuerde ihr Gewicht
            // umverteilt und kaputter Code besser bewertet.
            if (context.Compilation is null || !context.Compilation.Success)
            {
                return CheckerOutcome.WithTip(
                    "Da dein Code nicht kompiliert, konnten die Unit-Tests nicht ausgefuehrt werden.",
                    Failed("Unit-Tests ausgefuehrt", string.Empty));
            }

            await WriteTestFilesAsync(context.WorkingDirectory, testFiles, cancellationToken);

            var compilation = await CompileTestFilesAsync(context.WorkingDirectory, jarPath, testFiles, cancellationToken);
            if (!compilation.Success)
                return DescribeCompilationFailure(compilation);

            return await RunAsync(context.WorkingDirectory, jarPath, testFiles, cancellationToken);
        }

        private string ResolveJarPath() =>
            Path.IsPathRooted(_options.JUnitJarPath)
                ? _options.JUnitJarPath
                : Path.Combine(AppContext.BaseDirectory, _options.JUnitJarPath);

        private static async Task WriteTestFilesAsync(
            string workingDirectory,
            List<TaskUnitTestFile> testFiles,
            CancellationToken cancellationToken)
        {
            foreach (var file in testFiles)
            {
                // Wie bei der Abgabe: nur der reine Dateiname darf ins
                // Arbeitsverzeichnis, niemals ein Pfad.
                var fileName = Path.GetFileName(file.FileName);
                await File.WriteAllTextAsync(
                    Path.Combine(workingDirectory, fileName), file.Content, cancellationToken);
            }
        }

        private async Task<ProcessResult> CompileTestFilesAsync(
            string workingDirectory,
            string jarPath,
            List<TaskUnitTestFile> testFiles,
            CancellationToken cancellationToken)
        {
            var arguments = new List<string>
            {
                "-encoding", "UTF-8",
                "-J-Dstdout.encoding=UTF-8",
                "-J-Dstderr.encoding=UTF-8",

                // Path.PathSeparator statt ';' — unter Linux trennt ':', und in
                // Phase 7 laeuft das hier im Container.
                "-cp", $"{jarPath}{Path.PathSeparator}."
            };

            arguments.AddRange(testFiles.Select(file => Path.GetFileName(file.FileName)));

            return await _processRunner.RunAsync(
                new ProcessRequest(
                    "javac",
                    arguments,
                    workingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.CompileTimeoutSeconds)),
                cancellationToken);
        }

        // Die Testdatei passt nicht zur Abgabe. Das ist ein legitimes
        // Nichtbestehen — die Meldung muss aber sagen, was erwartet wurde.
        private CheckerOutcome DescribeCompilationFailure(ProcessResult compilation)
        {
            if (compilation.ExecutableNotFound)
                throw new InvalidOperationException(
                    "'javac' wurde nicht gefunden. Ohne JDK im PATH koennen Unit-Tests nicht geprueft werden.");

            if (compilation.TimedOut)
            {
                return CheckerOutcome.WithTip(
                    $"Das Kompilieren der Testdatei hat laenger als {_options.CompileTimeoutSeconds} Sekunden gebraucht.",
                    Failed("Testdatei kompiliert gegen deine Abgabe", string.Empty));
            }

            var rawOutput = string.IsNullOrWhiteSpace(compilation.StandardError)
                ? compilation.StandardOutput
                : compilation.StandardError;

            var explanation = JavaCompilerMessages.Translate(rawOutput);

            var tip = explanation is null
                ? "Die hinterlegten Tests lassen sich nicht gegen deine Abgabe uebersetzen. " +
                  "Pruefe, ob Klassen- und Methodennamen genau wie in der Aufgabenstellung geschrieben sind."
                : explanation;

            _logger.LogInformation("JUnit-Testdatei kompiliert nicht gegen die Abgabe: {Output}", rawOutput);

            // Rohausgabe anhaengen statt ersetzen: die Zeilennummer darin ist oft
            // der schnellste Weg zur Ursache.
            return CheckerOutcome.WithTip(
                tip,
                Failed("Testdatei kompiliert gegen deine Abgabe", rawOutput));
        }

        private async Task<CheckerOutcome> RunAsync(
            string workingDirectory,
            string jarPath,
            List<TaskUnitTestFile> testFiles,
            CancellationToken cancellationToken)
        {
            var reportsDirectory = Path.Combine(workingDirectory, ReportsDirectoryName);

            var arguments = new List<string>
            {
                // Ohne diese beiden Angaben schreibt die JVM unter Windows in der
                // Codepage des Systems, auch wenn die Ausgabe umgeleitet ist.
                "-Dstdout.encoding=UTF-8",
                "-Dstderr.encoding=UTF-8",
                "-jar", jarPath,
                "execute",
                "--class-path", ".",
                "--reports-dir", ReportsDirectoryName,
                "--disable-banner",
                "--disable-ansi-colors",
                "--details=none"
            };

            // Klassen ausdruecklich auswaehlen statt den Classpath zu durchsuchen:
            // in Java heisst die Datei wie die Klasse darin, das ist eindeutig.
            foreach (var file in testFiles)
            {
                arguments.Add("--select-class");
                arguments.Add(Path.GetFileNameWithoutExtension(file.FileName));
            }

            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "java",
                    arguments,
                    workingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.JUnitRunTimeoutSeconds)),
                cancellationToken);

            if (process.ExecutableNotFound)
                throw new InvalidOperationException(
                    "'java' wurde nicht gefunden. Ohne JDK im PATH koennen Unit-Tests nicht ausgefuehrt werden.");

            if (process.TimedOut)
            {
                return CheckerOutcome.WithTip(
                    $"Der Testlauf hat laenger als {_options.JUnitRunTimeoutSeconds} Sekunden gebraucht und wurde abgebrochen. " +
                    "Pruefe, ob eine Schleife nie endet oder auf eine Eingabe gewartet wird, die es nicht gibt.",
                    Failed("Unit-Tests ausgefuehrt", string.Empty));
            }

            // Ein Rueckgabewert ungleich 0 heisst hier nur "Tests sind
            // fehlgeschlagen" — die Wahrheit steht im Report.
            var testCases = JUnitReportReader.Read(reportsDirectory);

            if (testCases.Count == 0)
                return DescribeMissingReport(process);

            var results = testCases
                .Select(testCase => new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = testCase.DisplayName,
                    ExpectedOutput = string.Empty,
                    ActualOutput = testCase.Message,
                    Passed = testCase.Passed
                })
                .ToArray();

            return results.All(result => result.Passed)
                ? CheckerOutcome.Of(results)
                : CheckerOutcome.WithTip(
                    "Mindestens ein Unit-Test ist fehlgeschlagen. Die Meldung darunter nennt, was erwartet wurde.",
                    results);
        }

        // Kein Report trotz gelaufenem Prozess. Der haeufigste Grund ist ein
        // System.exit(...) in der Abgabe: das beendet die JVM des Testlaufs und
        // reisst alle uebrigen Testmethoden mit.
        private CheckerOutcome DescribeMissingReport(ProcessResult process)
        {
            _logger.LogWarning(
                "JUnit-Lauf hat keinen auswertbaren Report hinterlassen. ExitCode {ExitCode}, Ausgabe: {Output}",
                process.ExitCode,
                string.IsNullOrWhiteSpace(process.StandardError) ? process.StandardOutput : process.StandardError);

            return CheckerOutcome.WithTip(
                "Der Testlauf hat kein Ergebnis hinterlassen. Ruft dein Programm System.exit(...) auf? " +
                "Das beendet die virtuelle Maschine und bricht die Pruefung ab, bevor ein Ergebnis entsteht.",
                Failed("Unit-Tests ausgefuehrt", process.StandardOutput));
        }

        private static TestCaseResult Failed(string description, string actualOutput) => new()
        {
            Id = Guid.NewGuid(),
            Description = description,
            ExpectedOutput = string.Empty,
            ActualOutput = actualOutput,
            Passed = false
        };
    }
}

using System.ComponentModel;
using System.Diagnostics;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Models;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    public class CompilabilityChecker
    {
        private const int TimeoutMilliseconds = 30_000;

        public async Task<(CategoryResult Result, CompilationResult Compilation)> CheckAsync(List<SubmissionFile> files)
        {
            var workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop", Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDirectory);

            foreach (var file in files)
            {
                var filePath = Path.Combine(workingDirectory, file.FileName);
                await File.WriteAllTextAsync(filePath, file.Content);
            }

            var filePaths = files.Select(f => Path.Combine(workingDirectory, f.FileName));
            var arguments = string.Join(' ', filePaths.Select(p => $"\"{p}\""));

            var (exitCode, errorOutput) = await RunProcessAsync("javac", arguments, workingDirectory);
            var success = exitCode == 0;
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
                ActualOutput = success ? string.Empty : errorOutput
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

        // Sucht die Datei mit "public static void main" und gibt den dazugehoerigen
        // Klassennamen zurueck. In Java muss der Dateiname mit dem Klassennamen uebereinstimmen.
        private static string? FindMainClassName(List<SubmissionFile> files)
        {
            var mainFile = files.FirstOrDefault(f => f.Content.Contains("public static void main"));
            return mainFile is null ? null : Path.GetFileNameWithoutExtension(mainFile.FileName);
        }

        private static async Task<(int ExitCode, string ErrorOutput)> RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            try
            {
                using var process = Process.Start(startInfo)!;

                var errorTask = process.StandardError.ReadToEndAsync();
                var completed = process.WaitForExit(TimeoutMilliseconds);

                if (!completed)
                {
                    process.Kill(entireProcessTree: true);
                    return (-1, "Zeitueberschreitung beim Kompilieren.");
                }

                var errorOutput = await errorTask;
                return (process.ExitCode, errorOutput);
            }
            catch (Win32Exception)
            {
                return (-1, $"'{fileName}' wurde nicht gefunden. Ist das JDK installiert und im PATH?");
            }
        }
    }
}

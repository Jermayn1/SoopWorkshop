using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class CompilabilityCheckerTests : IDisposable
    {
        private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
        private readonly EvaluationOptions _options = new() { CompileTimeoutSeconds = 30 };
        private readonly string _workingDirectory;

        public CompilabilityCheckerTests()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_workingDirectory))
                Directory.Delete(_workingDirectory, recursive: true);
        }

        private CompilabilityChecker CreateChecker() => new(_processRunner, Options.Create(_options));

        private void JavacReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(result);

        [Fact]
        public async Task CheckAsync_KompilierungErfolgreich_LiefertVollePunkteUndHauptklasse()
        {
            JavacReturns(ProcessResultFactory.Success());

            var files = new List<Backend.Domain.Entities.SubmissionFile>
            {
                SubmissionFileFactory.Create("public class Main { public static void main(String[] a) {} }")
            };

            var (result, compilation) = await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.Compilability);
            compilation.Success.ShouldBeTrue();
            compilation.MainClassName.ShouldBe("Main");
            compilation.WorkingDirectory.ShouldBe(_workingDirectory);
        }

        [Fact]
        public async Task CheckAsync_KompilierungFehlgeschlagen_LiefertNullPunkteUndCompilerausgabe()
        {
            JavacReturns(ProcessResultFactory.Failure("Main.java:3: error: ';' expected"));

            var files = new List<Backend.Domain.Entities.SubmissionFile>
            {
                SubmissionFileFactory.Create("public class Main { kaputt }")
            };

            var (result, compilation) = await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(0);
            result.TestCaseResults.ShouldHaveSingleItem().ActualOutput.ShouldContain("';' expected");
            compilation.MainClassName.ShouldBeNull();
        }

        [Fact]
        public async Task CheckAsync_JavacFehlt_WeistAufDasFehlendeJdkHin()
        {
            JavacReturns(ProcessResultFactory.NotFound());

            var files = new List<Backend.Domain.Entities.SubmissionFile> { SubmissionFileFactory.Create("public class Main {}") };

            var (result, _) = await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            result.Passed.ShouldBeFalse();
            result.TestCaseResults.ShouldHaveSingleItem().ActualOutput.ShouldContain("JDK");
        }

        [Fact]
        public async Task CheckAsync_Zeitueberschreitung_NenntDieZeitgrenze()
        {
            JavacReturns(ProcessResultFactory.TimedOut());

            var files = new List<Backend.Domain.Entities.SubmissionFile> { SubmissionFileFactory.Create("public class Main {}") };

            var (result, _) = await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            result.TestCaseResults.ShouldHaveSingleItem().ActualOutput.ShouldContain("30 Sekunden");
        }

        [Fact]
        public async Task CheckAsync_MehrereDateien_UebergibtAlleAnJavac()
        {
            JavacReturns(ProcessResultFactory.Success());

            var files = SubmissionFileFactory.CreateMany("public class File1 {}", "public class File2 {}");

            await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "javac"
                                            && r.Arguments.Count(a => a.EndsWith(".java")) == 2),
                Arg.Any<CancellationToken>());

            File.Exists(Path.Combine(_workingDirectory, "File1.java")).ShouldBeTrue();
            File.Exists(Path.Combine(_workingDirectory, "File2.java")).ShouldBeTrue();
        }

        // javac stellt den uebergebenen Pfad seinen Fehlermeldungen voran. Mit
        // absoluten Pfaden las der Teilnehmer das Temp-Verzeichnis des Servers.
        [Fact]
        public async Task CheckAsync_UebergibtJavacNurDateinamenOhnePfad()
        {
            JavacReturns(ProcessResultFactory.Success());

            var files = new List<Backend.Domain.Entities.SubmissionFile> { SubmissionFileFactory.Create("public class Main {}") };

            await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.Arguments.Contains("Main.java")
                                            && r.Arguments.All(a => !a.Contains(_workingDirectory))
                                            && r.WorkingDirectory == _workingDirectory),
                Arg.Any<CancellationToken>());
        }

        // Zweite Verteidigungslinie hinter der Upload-Pruefung: ein Dateiname mit
        // Pfadanteilen darf nicht ausserhalb des Arbeitsverzeichnisses landen.
        [Theory]
        [InlineData("../Main.java")]
        [InlineData("..\\Main.java")]
        [InlineData("unterordner/Main.java")]
        public async Task CheckAsync_DateinameEnthaeltPfad_SchreibtNurInDasArbeitsverzeichnis(string fileName)
        {
            JavacReturns(ProcessResultFactory.Success());

            var files = new List<Backend.Domain.Entities.SubmissionFile>
            {
                SubmissionFileFactory.Create("public class Main {}", fileName)
            };

            await CreateChecker().CheckAsync(files, _workingDirectory, CancellationToken.None);

            Directory.GetFiles(_workingDirectory).ShouldHaveSingleItem()
                .ShouldBe(Path.Combine(_workingDirectory, "Main.java"));
        }
    }
}

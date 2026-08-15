using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Models;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class TestCaseCheckerTests
    {
        private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
        private readonly EvaluationOptions _options = new() { RunTimeoutSeconds = 10 };

        private TestCaseChecker CreateChecker() => new(_processRunner, Options.Create(_options));

        private static CompilationResult Compiled(string mainClassName = "Main") =>
            new() { Success = true, MainClassName = mainClassName, WorkingDirectory = "/tmp/egal" };

        private static TaskTest Test(string expectedOutput, string input = "", string description = "Testfall") =>
            new()
            {
                Id = Guid.NewGuid(),
                Input = input,
                ExpectedOutput = expectedOutput,
                Description = description
            };

        private void ProgramReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(result);

        [Fact]
        public async Task CheckAsync_AusgabeStimmtUeberein_LiefertVollePunktzahl()
        {
            ProgramReturns(ProcessResultFactory.Success("Hallo Soop"));

            var result = await CreateChecker().CheckAsync(Compiled(), [Test("Hallo Soop")], CancellationToken.None);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.TestCases);
            result.TestCaseResults.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Zeilenumbrueche und umgebende Leerzeichen sollen keinen Unterschied machen.
        [Theory]
        [InlineData("Hallo\r\nSoop", "Hallo\nSoop")]
        [InlineData("  Hallo Soop  ", "Hallo Soop")]
        [InlineData("Hallo Soop\n", "Hallo Soop")]
        public async Task CheckAsync_AusgabeWeichtNurInFormatierungAb_GiltAlsBestanden(string actual, string expected)
        {
            ProgramReturns(ProcessResultFactory.Success(actual));

            var result = await CreateChecker().CheckAsync(Compiled(), [Test(expected)], CancellationToken.None);

            result.Passed.ShouldBeTrue();
        }

        // Frueher lieferte eine Zeitueberschreitung eine leere Ausgabe — der Teilnehmer
        // sah nur einen roten Testfall ohne Erklaerung.
        [Fact]
        public async Task CheckAsync_ProgrammLaeuftZuLange_ErklaertDieZeitueberschreitung()
        {
            ProgramReturns(ProcessResultFactory.TimedOut());

            var result = await CreateChecker().CheckAsync(Compiled(), [Test("Hallo Soop")], CancellationToken.None);

            result.Passed.ShouldBeFalse();

            var testCase = result.TestCaseResults.ShouldHaveSingleItem();
            testCase.ActualOutput.ShouldContain("Zeitueberschreitung");
            testCase.ActualOutput.ShouldContain("10 Sekunden");
        }

        [Fact]
        public async Task CheckAsync_JavaFehlt_WeistAufDasFehlendeJdkHin()
        {
            ProgramReturns(ProcessResultFactory.NotFound());

            var result = await CreateChecker().CheckAsync(Compiled(), [Test("Hallo Soop")], CancellationToken.None);

            result.TestCaseResults.ShouldHaveSingleItem().ActualOutput.ShouldContain("JDK");
        }

        // Ein Laufzeitfehler ohne vorherige Ausgabe war bisher unsichtbar.
        [Fact]
        public async Task CheckAsync_ProgrammBrichtOhneAusgabeAb_ZeigtDieFehlerausgabe()
        {
            ProgramReturns(ProcessResultFactory.Failure(
                standardError: "Exception in thread \"main\" java.lang.NullPointerException"));

            var result = await CreateChecker().CheckAsync(Compiled(), [Test("Hallo Soop")], CancellationToken.None);

            result.TestCaseResults.ShouldHaveSingleItem().ActualOutput.ShouldContain("NullPointerException");
        }

        // Gibt das Programm etwas aus, zaehlt allein die Standardausgabe — sonst
        // wuerde eine Warnung auf stderr jeden Vergleich zerstoeren.
        [Fact]
        public async Task CheckAsync_ProgrammSchreibtAufBeideStroeme_VergleichtNurDieStandardausgabe()
        {
            ProgramReturns(ProcessResultFactory.Success("Hallo Soop", standardError: "Note: irgendeine Warnung"));

            var result = await CreateChecker().CheckAsync(Compiled(), [Test("Hallo Soop")], CancellationToken.None);

            result.Passed.ShouldBeTrue();
        }

        [Fact]
        public async Task CheckAsync_KompilierungFehlgeschlagen_MarkiertAlleTestfaelleAlsNichtBestanden()
        {
            var compilation = new CompilationResult { Success = false, MainClassName = null };

            var result = await CreateChecker().CheckAsync(
                compilation,
                [Test("A"), Test("B")],
                CancellationToken.None);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(0);
            result.TestCaseResults.Count.ShouldBe(2);
            result.TestCaseResults.ShouldAllBe(t => !t.Passed);

            await _processRunner.DidNotReceive().RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckAsync_MitEingabe_ReichtDieEingabeAnDenProzessDurch()
        {
            ProgramReturns(ProcessResultFactory.Success("7"));

            await CreateChecker().CheckAsync(Compiled(), [Test("7", input: "3\n4\n")], CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "java"
                                            && r.Arguments.Contains("Main")
                                            && r.StandardInput == "3\n4\n"),
                Arg.Any<CancellationToken>());
        }

        // Ist-Verhalten, bekannte Schwaeche: Aufgaben ohne Testfaelle geben die volle
        // Punktzahl geschenkt. Wird in Phase 3 mit dem Punktesystem v2 behoben (§9).
        [Fact]
        public async Task CheckAsync_KeineTestfaelle_LiefertVollePunktzahlOhnePruefung()
        {
            var result = await CreateChecker().CheckAsync(Compiled(), [], CancellationToken.None);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.TestCases);
        }

        // Ist-Verhalten, bekannte Schwaeche: 65 / 3 = 21, zwei bestandene Testfaelle
        // ergeben 42 statt der rechnerisch richtigen 43,33 (§9, Phase 3).
        [Fact]
        public async Task CheckAsync_ZweiVonDreiBestanden_VerliertPunkteDurchGanzzahlDivision()
        {
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>())
                .Returns(
                    ProcessResultFactory.Success("A"),
                    ProcessResultFactory.Success("B"),
                    ProcessResultFactory.Success("falsch"));

            var result = await CreateChecker().CheckAsync(
                Compiled(),
                [Test("A"), Test("B"), Test("C")],
                CancellationToken.None);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(42);
        }
    }
}

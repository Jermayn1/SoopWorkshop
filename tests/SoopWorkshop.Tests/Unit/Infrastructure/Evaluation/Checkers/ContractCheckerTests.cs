using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class ContractCheckerTests
    {
        private readonly ContractChecker _checker = new();

        private static TaskItem Task(string? expectedClassName, params string[] methodNames)
        {
            return new TaskItem
            {
                Id = Guid.NewGuid(),
                ExpectedClassName = expectedClassName,
                ExpectedMethods = [.. methodNames.Select((name, index) => new TaskExpectedMethod
                {
                    Id = Guid.NewGuid(),
                    Signature = $"public static int {name}(int a, int b)",
                    Name = name,
                    Order = index + 1
                })]
            };
        }

        private Task<CheckerOutcome> CheckAsync(TaskItem task, string code) =>
            _checker.CheckAsync(
                EvaluationContextFactory.For(task: task, files: [SubmissionFileFactory.Create(code)]),
                CancellationToken.None);

        [Fact]
        public void IsApplicable_OhneVorgaben_NichtAnwendbar()
        {
            _checker.IsApplicable(EvaluationContextFactory.For(task: Task(null))).ShouldBeFalse();
        }

        [Theory]
        [InlineData("Main")]
        [InlineData(null)]
        public void IsApplicable_MitVorgabe_IstAnwendbar(string? className)
        {
            var task = className is null ? Task(null, "addiere") : Task(className);

            _checker.IsApplicable(EvaluationContextFactory.For(task: task)).ShouldBeTrue();
        }

        // Der Fall, wegen dem es den Checker gibt: Java erzwingt nur, dass
        // Dateiname und Klassenname zusammenpassen - nicht, dass sie heissen wie
        // die Aufgabe verlangt.
        [Fact]
        public async Task CheckAsync_FalscherKlassenname_FaelltDurchUndNenntBeideNamen()
        {
            var outcome = await CheckAsync(Task("Main"), "public class Rechner { }");

            var result = outcome.Results.ShouldHaveSingleItem();
            result.Passed.ShouldBeFalse();
            result.ExpectedOutput.ShouldBe("Main");
            result.ActualOutput.ShouldBe("Rechner");
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CheckAsync_RichtigerKlassenname_Besteht()
        {
            var outcome = await CheckAsync(Task("Main"), "public class Main { }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();
        }

        [Theory]
        [InlineData("public interface Zaehler { }")]
        [InlineData("public enum Zaehler { EINS }")]
        [InlineData("public record Zaehler(int wert) { }")]
        public async Task CheckAsync_AndereBauform_ZaehltEbenfalls(string code)
        {
            var outcome = await CheckAsync(Task("Zaehler"), code);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Gross-/Kleinschreibung ist in Java bedeutsam - 'main' ist nicht 'Main'.
        [Fact]
        public async Task CheckAsync_KlassennameNurAndersGeschrieben_FaelltDurch()
        {
            var outcome = await CheckAsync(Task("Main"), "public class main { }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task CheckAsync_FehlendeMethode_NenntDieErwarteteSignatur()
        {
            var outcome = await CheckAsync(
                Task("Main", "addiere"),
                "public class Main { public static int addieren(int a, int b) { return a + b; } }");

            var method = outcome.Results.Single(result => result.Description.Contains("addiere("));
            method.Passed.ShouldBeFalse();

            outcome.ErrorTip.ShouldNotBeNull().ShouldContain("public static int addiere(int a, int b)");
        }

        [Fact]
        public async Task CheckAsync_VorhandeneMethode_Besteht()
        {
            var outcome = await CheckAsync(
                Task("Main", "addiere"),
                "public class Main { public static int addiere(int a, int b) { return a + b; } }");

            outcome.Results.ShouldAllBe(result => result.Passed);
            outcome.ErrorTip.ShouldBeNull();
        }

        [Fact]
        public async Task CheckAsync_MehrereMethoden_LiefertJeEineTeilpruefung()
        {
            var outcome = await CheckAsync(
                Task("Main", "addiere", "subtrahiere"),
                "public class Main { public static int addiere(int a, int b) { return a + b; } }");

            // Klasse plus zwei Methoden.
            outcome.Results.Count.ShouldBe(3);
            outcome.Results.Count(result => result.Passed).ShouldBe(2);
        }

        // Kommentare und Zeichenketten sind kein Code - eine dort erwaehnte
        // Klasse ist nicht deklariert.
        [Fact]
        public async Task CheckAsync_KlassennameNurImKommentar_FaelltDurch()
        {
            var outcome = await CheckAsync(
                Task("Main"),
                """
                // class Main waere hier richtig gewesen
                public class Rechner {
                    String hinweis = "class Main";
                }
                """);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        // Ist-Verhalten, bewusst hingenommen: geprueft wird die Anwesenheit des
        // Namens vor einer Klammer, nicht die vollstaendige Deklaration. Ein
        // blosser Aufruf zaehlt deshalb bereits als Treffer. Die exakte Signatur
        // prueft ohnehin der Compiler beim Uebersetzen der JUnit-Datei.
        [Fact]
        public async Task CheckAsync_MethodeNurAufgerufen_GiltBereitsAlsVorhanden()
        {
            var outcome = await CheckAsync(
                Task(null, "addiere"),
                "public class Main { void run() { addiere(1, 2); } }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Ein Aufruf auf einem Objekt zaehlt dagegen nicht - sonst wuerde jede
        // Nutzung einer fremden Bibliothek als eigene Methode durchgehen.
        [Fact]
        public async Task CheckAsync_MethodenaufrufAufFremdemObjekt_ZaehltNicht()
        {
            var outcome = await CheckAsync(
                Task(null, "addiere"),
                "public class Main { void run() { rechner.addiere(1, 2); } }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        [Fact]
        public void Category_IstKompilierbarkeit()
        {
            _checker.Category.ShouldBe(EvaluationCategory.Compilability);
        }
    }
}

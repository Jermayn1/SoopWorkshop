using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class NamingConventionCheckerTests
    {
        private readonly NamingConventionChecker _checker = new();

        private Task<CheckerOutcome> CheckAsync(IReadOnlyList<SubmissionFile> files) =>
            _checker.CheckAsync(EvaluationContextFactory.For(files: files), CancellationToken.None);

        // Reihenfolge der Teilpruefungen: 0 = Klassennamen, 1 = keine snake_case-Bezeichner.
        private static TestCaseResult ClassNames(CheckerOutcome outcome) => outcome.Results.ElementAt(0);

        private static TestCaseResult CamelCase(CheckerOutcome outcome) => outcome.Results.ElementAt(1);

        [Fact]
        public async Task CheckAsync_KorrekterCode_BestehtBeideTeilpruefungen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        int myValue = 1;
                        System.out.println(myValue);
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldAllBe(result => result.Passed);
            outcome.ErrorTip.ShouldBeNull();
        }

        [Fact]
        public async Task CheckAsync_KlasseInCamelCase_LaesstNurDieNamenspruefungDurchfallen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class greeter {
                    public static void main(String[] args) {
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            ClassNames(outcome).Passed.ShouldBeFalse();
            CamelCase(outcome).Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CheckAsync_SnakeCaseBezeichner_LaesstNurDieCamelCasePruefungDurchfallen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        int mein_wert = 1;
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            ClassNames(outcome).Passed.ShouldBeTrue();
            CamelCase(outcome).Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task CheckAsync_BeideVerstoesse_LaesstBeideTeilpruefungenDurchfallen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class greeter {
                    public static void main(String[] args) {
                        int mein_wert = 1;
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldAllBe(result => !result.Passed);
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        // Ohne Klassendeklaration gilt die PascalCase-Pruefung als bestanden,
        // weil es nichts zu pruefen gibt.
        [Fact]
        public async Task CheckAsync_OhneKlassendeklaration_BestehtBeideTeilpruefungen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public interface Greeter {
                    void greet();
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // SCREAMING_SNAKE_CASE ist fuer Java-Konstanten korrekt und wird
        // vom Regex bewusst nicht erfasst.
        [Fact]
        public async Task CheckAsync_ScreamingSnakeCaseKonstante_BestehtBeideTeilpruefungen()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static final int MAX_VALUE = 100;
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // Behebt das Finding aus §9: frueher schlug der Regex hier an, obwohl der
        // Code selbst einwandfrei war. Kommentare werden jetzt vorher entfernt.
        [Fact]
        public async Task CheckAsync_KlassennameImKommentar_WirdNichtMehrBeanstandet()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                // class beispiel zeigt, wie es nicht gemacht wird
                public class Greeter {
                    public static void main(String[] args) {
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            ClassNames(outcome).Passed.ShouldBeTrue();
        }

        [Fact]
        public async Task CheckAsync_KlassennameImBlockkommentar_WirdNichtMehrBeanstandet()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                /* Beispiel:
                   class beispiel { }
                */
                public class Greeter { }
                """);

            var outcome = await CheckAsync(files);

            ClassNames(outcome).Passed.ShouldBeTrue();
        }

        // Zweite Auspraegung desselben Findings: snake_case in einer Ausgabe sagt
        // nichts ueber die Benennung im Programm aus.
        [Fact]
        public async Task CheckAsync_SnakeCaseImStringLiteral_WirdNichtMehrBeanstandet()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        System.out.println("mein_wert");
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            CamelCase(outcome).Passed.ShouldBeTrue();
        }

        // Der Verstoss im echten Code darf durch das Entfernen der Literale
        // natuerlich nicht verschwinden.
        [Fact]
        public async Task CheckAsync_SnakeCaseImCodeUndImString_WirdWeiterhinBeanstandet()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        int mein_wert = 1;
                        System.out.println("mein_wert");
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            CamelCase(outcome).Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task CheckAsync_MehrereDateien_WertetAlleAus()
        {
            var files = SubmissionFileFactory.CreateMany(
                "public class Greeter { }",
                "public class helper { }");

            var outcome = await CheckAsync(files);

            ClassNames(outcome).Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task CheckAsync_Immer_LiefertZweiTeilpruefungen()
        {
            var files = SubmissionFileFactory.CreateMany("public class Greeter { }");

            var outcome = await CheckAsync(files);

            outcome.Results.Count.ShouldBe(2);
        }

        // Namenskonventionen sind seit der Bewertungs-Engine v2 eine Teilpruefung
        // unter Clean Code und keine eigene Kategorie mehr.
        [Fact]
        public void Category_IstCleanCode()
        {
            _checker.Category.ShouldBe(EvaluationCategory.CleanCode);
        }
    }
}

using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class NamingConventionCheckerTests
    {
        // Der Checker teilt seine Punkte auf zwei Teilpruefungen auf:
        // PascalCase-Klassennamen und "kein snake_case".
        private const int PointsPerCheck = EvaluationCategoryPoints.NamingConventions / 2;

        private readonly NamingConventionChecker _checker = new();

        [Fact]
        public void Check_KorrekterCode_LiefertVollePunktzahl()
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

            var result = _checker.Check(files);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.NamingConventions);
            result.ErrorTip.ShouldBeEmpty();
        }

        [Fact]
        public void Check_KlasseInCamelCase_LiefertHalbePunkte()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class greeter {
                    public static void main(String[] args) {
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(PointsPerCheck);
            result.TestCaseResults.ElementAt(0).Passed.ShouldBeFalse();
            result.TestCaseResults.ElementAt(1).Passed.ShouldBeTrue();
        }

        [Fact]
        public void Check_SnakeCaseBezeichner_LiefertHalbePunkte()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        int mein_wert = 1;
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(PointsPerCheck);
            result.TestCaseResults.ElementAt(0).Passed.ShouldBeTrue();
            result.TestCaseResults.ElementAt(1).Passed.ShouldBeFalse();
        }

        [Fact]
        public void Check_BeideVerstoesse_LiefertNullPunkte()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class greeter {
                    public static void main(String[] args) {
                        int mein_wert = 1;
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(0);
            result.ErrorTip.ShouldNotBeEmpty();
        }

        // Ohne Klassendeklaration gilt die PascalCase-Pruefung als bestanden,
        // weil es nichts zu pruefen gibt.
        [Fact]
        public void Check_OhneKlassendeklaration_LiefertVollePunktzahl()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public interface Greeter {
                    void greet();
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.NamingConventions);
        }

        // SCREAMING_SNAKE_CASE ist fuer Java-Konstanten korrekt und wird
        // vom Regex bewusst nicht erfasst.
        [Fact]
        public void Check_ScreamingSnakeCaseKonstante_LiefertVollePunktzahl()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static final int MAX_VALUE = 100;
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.NamingConventions);
        }

        // Ist-Verhalten, kein Wunschverhalten: der Regex arbeitet auf dem rohen Text
        // und findet "class" auch in einem Kommentar.
        // Siehe Finding vom 2026-08-15 (False Positives), geplant fuer Phase 3.
        [Fact]
        public void Check_KlassennameImKommentar_WirdMitgeprueft()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                // class beispiel zeigt, wie es nicht gemacht wird
                public class Greeter {
                    public static void main(String[] args) {
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Points.ShouldBe(PointsPerCheck);
            result.TestCaseResults.ElementAt(0).Passed.ShouldBeFalse();
        }

        // Ist-Verhalten, zweite Auspraegung desselben Findings: snake_case wird
        // auch innerhalb eines String-Literals beanstandet.
        [Fact]
        public void Check_SnakeCaseImStringLiteral_LiefertHalbePunkte()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Greeter {
                    public static void main(String[] args) {
                        System.out.println("mein_wert");
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Points.ShouldBe(PointsPerCheck);
            result.TestCaseResults.ElementAt(1).Passed.ShouldBeFalse();
        }

        [Fact]
        public void Check_MehrereDateien_WertetAlleAus()
        {
            var files = SubmissionFileFactory.CreateMany(
                "public class Greeter { }",
                "public class helper { }");

            var result = _checker.Check(files);

            result.Points.ShouldBe(PointsPerCheck);
            result.TestCaseResults.ElementAt(0).Passed.ShouldBeFalse();
        }

        [Fact]
        public void Check_Immer_LiefertKategorieUndZweiTeilergebnisse()
        {
            var files = SubmissionFileFactory.CreateMany("public class Greeter { }");

            var result = _checker.Check(files);

            result.Category.ShouldBe(EvaluationCategory.NamingConventions);
            result.MaxPoints.ShouldBe(EvaluationCategoryPoints.NamingConventions);
            result.TestCaseResults.Count.ShouldBe(2);
        }
    }
}

using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class CharacterSetCheckerTests
    {
        private readonly CharacterSetChecker _checker = new();

        // Ohne Dateien gibt es nichts zu beanstanden. Anders als beim TestCaseChecker
        // sind das keine Gratispunkte, denn ohne Code gibt es auch keinen Verstoss.
        [Fact]
        public void Check_OhneDateien_LiefertVollePunktzahl()
        {
            var result = _checker.Check([]);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.CharacterSet);
        }

        [Fact]
        public void Check_CodeOhneUmlaute_LiefertVollePunktzahl()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("Hallo Soop");
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.CharacterSet);
            result.ErrorTip.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("ä")]
        [InlineData("ö")]
        [InlineData("ü")]
        [InlineData("Ä")]
        [InlineData("Ö")]
        [InlineData("Ü")]
        [InlineData("ß")]
        public void Check_VerbotenesZeichen_LiefertNullPunkte(string character)
        {
            var files = SubmissionFileFactory.CreateMany(
                $$"""
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("{{character}}");
                    }
                }
                """);

            var result = _checker.Check(files);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(0);
            result.ErrorTip.ShouldNotBeEmpty();
        }

        // Ist-Verhalten: geprueft wird nur der Dateiinhalt, nicht der Dateiname.
        [Fact]
        public void Check_UmlautNurImDateinamen_LiefertVollePunktzahl()
        {
            var files = new List<SubmissionFile>
            {
                SubmissionFileFactory.Create("public class Gruesse { }", "Grüße.java")
            };

            var result = _checker.Check(files);

            result.Passed.ShouldBeTrue();
            result.Points.ShouldBe(EvaluationCategoryPoints.CharacterSet);
        }

        [Fact]
        public void Check_UmlautInZweiterDatei_LiefertNullPunkte()
        {
            var files = SubmissionFileFactory.CreateMany(
                "public class Main { }",
                "public class Helper { String text = \"Grüße\"; }");

            var result = _checker.Check(files);

            result.Passed.ShouldBeFalse();
            result.Points.ShouldBe(0);
        }

        [Fact]
        public void Check_Immer_LiefertKategorieUndEinTeilergebnis()
        {
            var files = SubmissionFileFactory.CreateMany("public class Main { }");

            var result = _checker.Check(files);

            result.Category.ShouldBe(EvaluationCategory.CharacterSet);
            result.MaxPoints.ShouldBe(EvaluationCategoryPoints.CharacterSet);
            result.TestCaseResults.Count.ShouldBe(1);
            result.TestCaseResults.Single().Passed.ShouldBeTrue();
        }
    }
}

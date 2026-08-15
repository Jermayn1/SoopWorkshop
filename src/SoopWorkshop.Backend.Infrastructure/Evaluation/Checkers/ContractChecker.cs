using System.Text.RegularExpressions;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Prueft, ob die Abgabe die von der Aufgabe geforderten Namen ueberhaupt
    // verwendet - die Klasse und die verlangten Methoden.
    //
    // Warum eigens: Java erzwingt nur, dass Dateiname und Klassenname
    // zusammenpassen, nicht dass sie heissen wie gefordert. Eine Aufgabe verlangt
    // 'Main' mit 'addiere', jemand gibt 'Rechner.java' mit 'class Rechner' ab -
    // das kompiliert, die Konsolen-Testfaelle laufen durch, und die Abgabe besteht.
    // Bei JUnit faellt es zwar auf, aber erst als Compilerfehler.
    //
    // Laeuft vor dem Kompilieren und braucht nur den Quelltext.
    public class ContractChecker : IEvaluationChecker
    {
        // Teil der Kompilierbarkeit: hier geht es darum, ob der Code ueberhaupt
        // zur Aufgabe passt - nicht um Stil und nicht um das Ergebnis.
        public EvaluationCategory Category => EvaluationCategory.Compilability;

        public int Order => EvaluationCheckerOrder.Contract;

        // Nur wenn die Aufgabe ueberhaupt Namen vorgibt.
        public bool IsApplicable(EvaluationContext context) =>
            !string.IsNullOrWhiteSpace(context.Task.ExpectedClassName)
            || context.Task.ExpectedMethods.Count > 0;

        public Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            // Nur echter Code: eine Klasse, die bloss im Kommentar erwaehnt wird,
            // ist nicht deklariert.
            var code = string.Join(
                "\n",
                context.Files.Select(file => JavaSourceText.StripCommentsAndLiterals(file.Content)));

            var results = new List<TestCaseResult>();
            var missing = new List<string>();

            var expectedClassName = context.Task.ExpectedClassName;
            if (!string.IsNullOrWhiteSpace(expectedClassName))
            {
                var found = DeclaresType(code, expectedClassName);

                results.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = $"Klasse '{expectedClassName}' vorhanden",
                    ExpectedOutput = expectedClassName,
                    ActualOutput = found ? expectedClassName : DescribeFoundTypes(code),
                    Passed = found
                });

                if (!found)
                    missing.Add($"die Klasse '{expectedClassName}'");
            }

            foreach (var method in context.Task.ExpectedMethods.OrderBy(method => method.Order))
            {
                var found = DeclaresMethod(code, method.Name);

                results.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = $"Methode '{method.Signature}' vorhanden",
                    ExpectedOutput = method.Signature,
                    ActualOutput = found ? method.Signature : string.Empty,
                    Passed = found
                });

                if (!found)
                    missing.Add($"die Methode '{method.Signature}'");
            }

            if (missing.Count == 0)
                return Task.FromResult(CheckerOutcome.Of([.. results]));

            return Task.FromResult(CheckerOutcome.WithTip(
                $"Deine Abgabe enthaelt {string.Join(" und ", missing)} nicht. " +
                "Die Aufgabenstellung gibt die Namen genau vor - achte auf Schreibweise " +
                "und Gross-/Kleinschreibung.",
                [.. results]));
        }

        // class, interface, enum und record zaehlen gleichermassen - welche Bauform
        // die Aufgabe verlangt, steht in ihrer Beschreibung.
        private static bool DeclaresType(string code, string typeName) =>
            Regex.IsMatch(code, $@"\b(?:class|interface|enum|record)\s+{Regex.Escape(typeName)}\b");

        // Geprueft wird die reine Anwesenheit des Namens vor einer Klammer.
        // Ist-Verhalten: ein blosser Aufruf 'addiere(1, 2)' im selben Quelltext
        // zaehlt bereits als Treffer. Das ist hingenommen - wer die Methode
        // aufruft, hat sie meistens auch. Die exakte Signatur prueft ohnehin der
        // Java-Compiler beim Uebersetzen der JUnit-Datei.
        private static bool DeclaresMethod(string code, string methodName) =>
            !string.IsNullOrWhiteSpace(methodName)
            && Regex.IsMatch(code, $@"(?<![.\w]){Regex.Escape(methodName)}\s*\(");

        // Hilft dem Teilnehmer, den Unterschied selbst zu sehen: "erwartet Main,
        // gefunden Rechner".
        private static string DescribeFoundTypes(string code)
        {
            var found = Regex.Matches(code, @"\b(?:class|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .ToList();

            return found.Count == 0 ? string.Empty : string.Join(", ", found);
        }
    }
}

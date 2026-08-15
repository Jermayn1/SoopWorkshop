using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Was ein Checker zurueckgibt: bestandene und nicht bestandene Teilpruefungen,
    // dazu ein Hinweis fuer den Teilnehmer.
    //
    // Bewusst ohne Punkte. Die Punkteberechnung gehoert in den EvaluationScorer -
    // solange jeder Checker selbst gerechnet hat, gab es vier Stellen mit vier
    // Rundungsfehlern und keine Moeglichkeit, pro Aufgabe zu gewichten.
    public sealed record CheckerOutcome(IReadOnlyList<TestCaseResult> Results, string? ErrorTip)
    {
        public static CheckerOutcome Of(params TestCaseResult[] results) =>
            new(results, null);

        public static CheckerOutcome WithTip(string errorTip, params TestCaseResult[] results) =>
            new(results, errorTip);
    }
}

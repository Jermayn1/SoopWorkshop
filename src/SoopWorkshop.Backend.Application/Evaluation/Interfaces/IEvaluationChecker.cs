using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    // Eine Pruefung innerhalb der Auswertung. Neue Pruefungen werden nur noch
    // registriert, der JavaAnalyzer muss dafuer nicht mehr angefasst werden.
    public interface IEvaluationChecker
    {
        // Mehrere Checker duerfen dieselbe Kategorie liefern - Clean Code besteht
        // genau so aus mehreren unabhaengigen Teilpruefungen.
        EvaluationCategory Category { get; }

        // Ausfuehrungsreihenfolge, nicht Anzeigereihenfolge (die steht in
        // EvaluationCategoryOrder). Wer das Kompilierergebnis aus dem Kontext
        // braucht, muss hinter dem CompilabilityChecker liegen.
        int Order { get; }

        // Ob die Pruefung fuer diese Aufgabe ueberhaupt gilt.
        //
        // WICHTIG: Diese Frage haengt allein an der Aufgabendefinition - Modus,
        // hinterlegte Testfaelle, hinterlegte JUnit-Dateien. Niemals am Ergebnis
        // des Laufs. Wuerde eine nicht kompilierende Abgabe hier als "nicht
        // anwendbar" gelten, fiele ihre Kategorie aus der Wertung, das Gewicht
        // wuerde auf die uebrigen verteilt - und kaputter Code bekaeme eine
        // bessere Note als halb funktionierender.
        bool IsApplicable(EvaluationContext context);

        Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken);
    }
}

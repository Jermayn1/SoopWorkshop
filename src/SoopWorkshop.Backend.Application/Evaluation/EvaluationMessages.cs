namespace SoopWorkshop.Backend.Application.Evaluation
{
    // Hinweistexte, die sich mehrere Checker teilen.
    //
    // Warum geteilt: Konsolen-Testfaelle und Unit-Tests zahlen auf dieselbe
    // Kategorie ein. Haette jeder seinen eigenen Wortlaut, stuenden bei einer
    // Aufgabe mit beiden Pruefarten zwei Hinweise hintereinander, die dasselbe
    // sagen - der Teilnehmer liest dann einen Absatz statt eines Satzes.
    public static class EvaluationMessages
    {
        public const string ComparisonHint =
            "Vergleiche 'Erwartet' und 'Erhalten' genau - Gross-/Kleinschreibung, " +
            "Leerzeichen und Zeilenumbrueche zaehlen mit.";
    }
}

namespace SoopWorkshop.Backend.Application.Evaluation
{
    // Ausfuehrungsreihenfolge der Checker. Die Abstaende lassen Platz, damit eine
    // neue Pruefung dazwischen passt, ohne bestehende Werte zu verschieben.
    //
    // Kompiliert wird zuerst: der CompilabilityChecker legt das Kompilierergebnis
    // im EvaluationContext ab, alles was die Abgabe ausfuehrt braucht es.
    // Die Anzeigereihenfolge ist eine andere und steht in EvaluationCategoryOrder.
    public static class EvaluationCheckerOrder
    {
        public const int Compilability = 10;
        public const int CharacterSet = 20;
        public const int NamingConventions = 30;
        public const int TestCases = 40;
        public const int UnitTests = 50;
    }
}

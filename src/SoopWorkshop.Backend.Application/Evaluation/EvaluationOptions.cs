namespace SoopWorkshop.Backend.Application.Evaluation
{
    // Stellschrauben der Auswertung. Gebunden an den Konfigurationsabschnitt "Evaluation",
    // die Standardwerte hier gelten, wenn nichts konfiguriert ist.
    public class EvaluationOptions
    {
        public const string SectionName = "Evaluation";

        // Wie viele Abgaben gleichzeitig ausgewertet werden. Begrenzt die Anzahl
        // paralleler javac-/java-Prozesse auf dem Server.
        public int MaxConcurrency { get; set; } = 2;

        public int CompileTimeoutSeconds { get; set; } = 30;

        public int RunTimeoutSeconds { get; set; } = 10;

        // Obergrenze der Warteschlange. Ist sie voll, wartet das Einreihen,
        // statt unbegrenzt Arbeit anzusammeln.
        public int QueueCapacity { get; set; } = 100;
    }
}

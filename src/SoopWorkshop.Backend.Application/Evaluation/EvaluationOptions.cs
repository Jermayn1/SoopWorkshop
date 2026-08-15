using SoopWorkshop.Shared.Enums;

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

        // Pfad zum JUnit-Platform-Console-Standalone-JAR. Relative Angaben werden
        // gegen das Anwendungsverzeichnis aufgeloest.
        public string JUnitJarPath { get; set; } = "lib/junit-platform-console-standalone-6.1.3.jar";

        // Zeitgrenze fuer den JUnit-Lauf. Grosszuegiger als RunTimeoutSeconds, weil
        // ein Lauf alle Testmethoden einer Aufgabe auf einmal abarbeitet und die
        // JVM samt Platform erst hochfahren muss.
        public int JUnitRunTimeoutSeconds { get; set; } = 30;

        // Standardgewichte der Bewertungskategorien. Nicht in Punkten, sondern
        // relativ zueinander - erst die Normierung im EvaluationScorer macht
        // daraus die erreichbaren Punkte. Eine Aufgabe kann einzelne Gewichte
        // ueber TaskCategoryWeight ueberschreiben.
        //
        // Die Werte sind so gewaehlt, dass eine reine Konsolenaufgabe genau die
        // Verteilung von vorher behaelt (15 = 5 Zeichensatz + 10 Namenskonventionen).
        public Dictionary<EvaluationCategory, double> CategoryWeights { get; set; } = new()
        {
            [EvaluationCategory.CleanCode] = 15,
            [EvaluationCategory.Compilability] = 20,
            [EvaluationCategory.TestCases] = 65,
            [EvaluationCategory.UnitTests] = 65
        };
    }
}

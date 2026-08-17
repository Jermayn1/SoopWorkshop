using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.Constants
{
    // Feste Anzeigereihenfolge der Kategorien. Bewusst getrennt von der
    // Ausfuehrungsreihenfolge der Checker: kompiliert wird zuerst, angezeigt wird
    // aber mit Clean Code begonnen. Ohne diese Liste haengt die Reihenfolge im
    // Frontend davon ab, wie die Datenbank die Zeilen zurueckgibt.
    public static class EvaluationCategoryOrder
    {
        private static readonly EvaluationCategory[] DisplayOrder =
        [
            EvaluationCategory.CleanCode,
            EvaluationCategory.Compilability,
            EvaluationCategory.Functionality
        ];

        // Dieselbe Liste, aber als Aussage darueber, welche Kategorien ueberhaupt
        // noch vergeben werden. Die uebrigen Enum-Werte sind Altlast (§5.6) und
        // stehen nur noch da, weil sie als int in der Datenbank liegen.
        //
        // Wer aufgabenspezifische Gewichte setzt, darf nur diese treffen: ein
        // Gewicht auf TestCases wuerde nie gelesen und waere damit stille
        // Konfiguration, die nichts tut.
        public static IReadOnlyList<EvaluationCategory> Active => DisplayOrder;

        public static bool IsActive(EvaluationCategory category) =>
            Array.IndexOf(DisplayOrder, category) >= 0;

        // Kategorien, die nicht mehr vergeben werden (Zeichensatz, Namenskonventionen,
        // Testfaelle und Unit-Tests aus frueheren Auswertungen), landen hinten statt
        // zu verschwinden.
        public static int Of(EvaluationCategory category)
        {
            var index = Array.IndexOf(DisplayOrder, category);
            return index < 0 ? DisplayOrder.Length : index;
        }
    }
}

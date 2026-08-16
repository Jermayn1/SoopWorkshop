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

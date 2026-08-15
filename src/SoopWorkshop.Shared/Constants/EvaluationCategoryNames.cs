namespace SoopWorkshop.Shared.Constants
{
    // Für das Frontend die Anzeigenamen der Bewertungskategorien
    public static class EvaluationCategoryNames
    {
        // Werden nicht mehr vergeben, kommen aber in frueheren Auswertungen vor
        // und brauchen deshalb weiterhin einen Anzeigenamen.
        public const string CharacterSet = "Zeichensatz";
        public const string NamingConventions = "Namenskonventionen";

        public const string Compilability = "Kompilierbarkeit";
        public const string CleanCode = "Clean Code";
        public const string TestCases = "Testfälle";
        public const string UnitTests = "Unit-Tests";
    }
}

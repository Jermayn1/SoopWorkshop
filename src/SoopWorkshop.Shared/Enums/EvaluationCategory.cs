namespace SoopWorkshop.Shared.Enums
{
    // Die Werte liegen als int in der Datenbank. Neue Kategorien werden deshalb
    // ausschliesslich angehaengt - wird umsortiert, deutet das Altbestaende um.
    public enum EvaluationCategory
    {
        // Altlast: seit der Bewertungs-Engine v2 sind Zeichensatz und
        // Namenskonventionen Teilpruefungen unter CleanCode. Die beiden Werte
        // werden nicht mehr vergeben, bleiben aber stehen, damit fruehere
        // Auswertungen weiterhin richtig gelesen werden. Nicht wiederverwenden.
        CharacterSet,
        NamingConventions,

        Compilability,
        CleanCode,
        TestCases,

        // Aufgaben-Unittests (JUnit), getrennt von den Konsolen-Testfaellen.
        UnitTests,
    }
}

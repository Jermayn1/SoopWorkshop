namespace SoopWorkshop.Backend.Domain.Entities
{
    public class TestCaseResult
    {
        public Guid Id { get; set; }
        public Guid CategoryResultId { get; set; }
        public string Description { get; set; } = string.Empty;

        // Womit die Pruefung gefuettert wurde - bei Konsolen-Testfaellen die
        // Eingabe. Ohne sie steht in der Anzeige "erwartet 7, erhalten 5", ohne
        // dass jemand sieht, mit welchen Werten gerechnet wurde.
        public string Input { get; set; } = string.Empty;

        public string ExpectedOutput { get; set; } = string.Empty;
        public string ActualOutput { get; set; } = string.Empty;
        public bool Passed { get; set; }

        // Anzeigereihenfolge innerhalb der Kategorie. Ohne sie bestimmt die
        // Datenbank, in welcher Reihenfolge die Teilpruefungen zurueckkommen.
        public int Order { get; set; }

        public CategoryResult CategoryResult { get; set; } = null!;
    }
}
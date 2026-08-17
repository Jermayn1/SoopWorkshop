using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    // Setzt alle Konsolen-Testfaelle einer Aufgabe in einem Aufruf, wie es
    // SaveTaskUnitTestFilesDto fuer die JUnit-Dateien tut - was hier nicht
    // drinsteht, wird geloescht.
    //
    // Der Editor bearbeitet die Testfaelle einer Aufgabe als Block. Ohne diesen
    // Endpunkt muesste er pro Zeile einzeln anlegen, aendern und loeschen und
    // koennte mitten in der Folge scheitern - die Aufgabe stuende dann mit einer
    // halb gespeicherten Pruefung da.
    public class SaveTaskTestsDto
    {
        [Required]
        public Guid TaskItemId { get; set; }

        public List<SaveTaskTestEntryDto> Tests { get; set; } = [];
    }

    public class SaveTaskTestEntryDto
    {
        // Darf leer sein: eine Aufgabe ohne Eingabe hat keine.
        public string Input { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die erwartete Ausgabe ist erforderlich.")]
        public string ExpectedOutput { get; set; } = string.Empty;

        // Der Satz, den der Teilnehmer als Beschreibung der Teilpruefung liest —
        // Wortlaut nach §5.7: eine Aussage ueber die Abgabe, nicht ueber das
        // Ergebnis ("Das Programm addiert zwei positive Zahlen").
        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        [MaxLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
    }
}

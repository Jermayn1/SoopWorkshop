using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    // Eine geforderte Klasse beim Anlegen oder Aendern einer Aufgabe.
    //
    // Die Methoden stehen bewusst INNERHALB der Klasse und nicht daneben: nur so
    // laesst sich pruefen, ob 'einzahlen' auch wirklich in 'Konto' steht und
    // nicht irgendwo sonst in der Abgabe.
    public class ExpectedTypeInputDto
    {
        [Required(ErrorMessage = "Der Klassenname ist erforderlich.")]
        [MaxLength(200, ErrorMessage = "Der Klassenname darf maximal 200 Zeichen lang sein.")]
        public string Name { get; set; } = string.Empty;

        // Je Eintrag eine Signatur, wie sie in der Aufgabenstellung steht.
        // Der gepruefte Methodenname wird daraus abgeleitet.
        public List<string> Methods { get; set; } = [];
    }
}

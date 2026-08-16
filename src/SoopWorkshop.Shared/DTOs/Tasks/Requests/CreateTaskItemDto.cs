using System.ComponentModel.DataAnnotations;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskItemDto
    {
        [Required]
        public Guid TaskCategoryId { get; set; }

        [Required(ErrorMessage = "Der Titel ist erforderlich.")]
        [MaxLength(200, ErrorMessage = "Der Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        public string Description { get; set; } = string.Empty;
        
        public Difficulty Difficulty { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }

        // Standard ConsoleOnly - passt zu den fruehen Aufgaben und laesst sich
        // ohne hinterlegte JUnit-Datei speichern.
        public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.ConsoleOnly;

        // Leer lassen, wenn die Aufgabe keinen bestimmten Klassennamen verlangt.
        [MaxLength(200, ErrorMessage = "Der Klassenname darf maximal 200 Zeichen lang sein.")]
        public string? ExpectedClassName { get; set; }

        // Je Eintrag eine Signatur, wie sie in der Aufgabenstellung steht.
        // Der geprüfte Methodenname wird daraus abgeleitet.
        public List<string> ExpectedMethods { get; set; } = [];

        // Erspart den zusaetzlichen PATCH-Aufruf nach dem Anlegen.
        public bool IsVisible { get; set; }

        public List<string> Hints { get; set; } = [];
    }
}
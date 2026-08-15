using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks
{
    public class TaskItemDto
    {
        public Guid Id { get; set; }
        public Guid TaskCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public EvaluationMode EvaluationMode { get; set; }

        // Welche Klassen und Methoden erwartet werden, damit die hinterlegten
        // JUnit-Tests gegen die Abgabe kompilieren.
        public string? ExpectedSignatures { get; set; }

        public List<TaskHintDto> Hints { get; set; } = [];

        // Nur die Dateien, die fuer Teilnehmer freigeschaltet sind. Im
        // Admin-Bereich kommen sie ueber den eigenen Endpunkt vollstaendig.
        public List<TaskUnitTestFileDto> VisibleUnitTestFiles { get; set; } = [];
    }
}
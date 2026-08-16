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

        // Der Vertrag: wie die Klasse heissen muss und welche Methoden erwartet
        // werden. Wird geprueft und dem Teilnehmer angezeigt.
        public string? ExpectedClassName { get; set; }

        // Vollstaendige Signaturen zur Anzeige, z. B.
        // "public static int addiere(int ersteZahl, int zweiteZahl)".
        public List<string> ExpectedMethods { get; set; } = [];

        public List<TaskHintDto> Hints { get; set; } = [];

        // Nur die Dateien, die fuer Teilnehmer freigeschaltet sind. Im
        // Admin-Bereich kommen sie ueber den eigenen Endpunkt vollstaendig.
        public List<TaskUnitTestFileDto> VisibleUnitTestFiles { get; set; } = [];
    }
}
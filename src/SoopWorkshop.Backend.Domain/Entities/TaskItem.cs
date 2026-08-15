using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid TaskCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; }

    // Womit geprueft wird. Standard ist die Konsolenpruefung, mit der die
    // fruehen Aufgaben auskommen.
    public EvaluationMode EvaluationMode { get; set; } = EvaluationMode.ConsoleOnly;

    // Der Vertrag zwischen Aufgabe und Abgabe: welche Klassen und Methoden mit
    // welcher Signatur erwartet werden. Ohne das steht er nur im Fliesstext der
    // Beschreibung und geht unter - und die JUnit-Datei kompiliert nicht.
    public string? ExpectedSignatures { get; set; }

    public TaskCategory Category { get; set; } = null!;
    public ICollection<TaskHint> Hints { get; set; } = [];
    public ICollection<TaskTest> Tests { get; set; } = [];
    public ICollection<TaskUnitTestFile> UnitTestFiles { get; set; } = [];
    public ICollection<Submission> Submissions { get; set; } = [];

    // Leer, solange die Standardgewichte aus der Konfiguration gelten sollen.
    public ICollection<TaskCategoryWeight> CategoryWeights { get; set; } = [];
}
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

    public TaskCategory Category { get; set; } = null!;
    public ICollection<TaskHint> Hints { get; set; } = [];
    public ICollection<TaskTest> Tests { get; set; } = [];
    public ICollection<Submission> Submissions { get; set; } = [];
}
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
        public List<TaskHintDto> Hints { get; set; } = [];
    }
}
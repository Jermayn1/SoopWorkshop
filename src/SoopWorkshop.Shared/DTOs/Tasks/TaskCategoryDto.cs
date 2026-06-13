namespace SoopWorkshop.Shared.DTOs.Tasks
{
    public class TaskCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public List<TaskItemDto> Tasks { get; set; } = [];
    }
}
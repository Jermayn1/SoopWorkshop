namespace SoopWorkshop.Shared.DTOs.Admin
{
    public class UpdateTaskCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }
    }
}
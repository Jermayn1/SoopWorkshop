namespace SoopWorkshop.Shared.DTOs.Admin
{
    public class CreateTaskCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
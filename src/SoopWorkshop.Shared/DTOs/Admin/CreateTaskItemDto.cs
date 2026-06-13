using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Admin
{
    public class CreateTaskItemDto
    {
        public Guid TaskCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; }
        public int Order { get; set; }
        public List<string> Hints { get; set; } = [];
    }
}
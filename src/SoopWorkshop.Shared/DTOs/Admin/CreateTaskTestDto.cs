namespace SoopWorkshop.Shared.DTOs.Admin
{
    public class CreateTaskTestDto
    {
        public Guid TaskItemId { get; set; }
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
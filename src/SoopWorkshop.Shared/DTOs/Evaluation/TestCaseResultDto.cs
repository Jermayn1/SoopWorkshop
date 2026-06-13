namespace SoopWorkshop.Shared.DTOs.Evaluation
{
    public class TestCaseResultDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string ActualOutput { get; set; } = string.Empty;
        public bool Passed { get; set; }
    }
}
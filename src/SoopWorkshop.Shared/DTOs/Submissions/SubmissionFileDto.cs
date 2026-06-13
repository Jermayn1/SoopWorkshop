namespace SoopWorkshop.Shared.DTOs.Submissions
{
    public class SubmissionFileDto
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
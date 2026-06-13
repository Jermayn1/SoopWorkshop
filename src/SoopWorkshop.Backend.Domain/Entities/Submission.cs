using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities
{
    public class Submission
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        public TaskItem Task { get; set; } = null!;
        public ICollection<SubmissionFile> Files { get; set; } = [];
        public EvaluationResult? EvaluationResult { get; set; }
    }
}
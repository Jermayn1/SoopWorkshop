using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;

namespace SoopWorkshop.Backend.Application.Submissions.Interfaces
{
    public interface ISubmissionService
    {
        Task<Result<SubmissionDto>> CreateAsync(Guid taskItemId, List<(string FileName, string Content)> files);
        Task<Result<EvaluationResultDto>> GetResultAsync(Guid submissionId);
    }
}
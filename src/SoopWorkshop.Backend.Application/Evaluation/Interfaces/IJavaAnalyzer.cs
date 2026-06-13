using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Evaluation.Interfaces
{
    public interface IJavaAnalyzer
    {
        // Analysiert die Liste von hochgeladen .java Dateien und gibt ein EvaluationResult zurück
        Task<EvaluationResult> AnalyzeAsync(Submission submission, List<TaskTest> expectedTests);
    }
}
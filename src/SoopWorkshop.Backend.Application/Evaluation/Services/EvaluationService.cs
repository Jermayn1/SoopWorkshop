using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Evaluation.Services
{
    // Steuert den Ablauf der Auswertung
    // 1. Läd die Submission
    // 2. Ruft den Java Analyzerauf
    // 3. speichert das Ergebnis und aktualisiert den Status
    public class EvaluationService : IEvaluationService
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IEvaluationResultRepository _evaluationResultRepository;
        private readonly IJavaAnalyzer _javaAnalyzer;

        public EvaluationService(
            ISubmissionRepository submissionRepository,
            IEvaluationResultRepository evaluationResultRepository,
            IJavaAnalyzer javaAnalyzer)
        {
            _submissionRepository = submissionRepository;
            _evaluationResultRepository = evaluationResultRepository;
            _javaAnalyzer = javaAnalyzer;
        }

        public async Task EvaluateAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission is null)
                return;

            submission.Status = SubmissionStatus.Running;
            await _submissionRepository.UpdateAsync(submission);

            try
            {
                var expectedTests = submission.Task.Tests.ToList();
                var evaluationResult = await _javaAnalyzer.AnalyzeAsync(submission, expectedTests, cancellationToken);

                await _evaluationResultRepository.AddAsync(evaluationResult);

                submission.Status = SubmissionStatus.Done;
            }
            catch (Exception)
            {
                submission.Status = SubmissionStatus.Failed;
            }

            await _submissionRepository.UpdateAsync(submission);
        }
    }
}
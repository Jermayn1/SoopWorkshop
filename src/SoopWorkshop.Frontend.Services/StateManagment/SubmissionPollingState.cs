using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Shared.DTOs.Evaluation;

namespace SoopWorkshop.Frontend.Services.StateManagment
{
    // Fragt alle 2 Sekunden nach dem Auswertungsstatus einer Submission
    public class SubmissionPollingState : IAsyncDisposable
    {
        private readonly SubmissionApiClient _submissionApiClient;
        private CancellationTokenSource? _cts;

        // Sobald ein Ergebnis vorliegt abboniert das UI das Event und ruft StatehasChanged() auf, um neu zu rendern
        public event Action<EvaluationResultDto>? OnResultReceived;

        // Wird aufgerufen, wenn die Auswertung fehlschlägt
        public event Action<string>? OnError;
        
        public EvaluationResultDto? CurrentResult { get; private set; }
        public bool IsPolling { get; private set; }
        
        public SubmissionPollingState(SubmissionApiClient submissionApiClient)
        {
            _submissionApiClient = submissionApiClient;
        }
        
        // Startet die Auswertung für eine Submission
        public void StartPolling(Guid submissionId)
        {
            StopPolling();
            
            CurrentResult = null;
            IsPolling = true;
            _cts = new CancellationTokenSource();
            
            _ = PollAsync(submissionId, _cts.Token);
        }
        
        public void StopPolling()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            IsPolling = false;
        }

        private async Task PollAsync(Guid submissionId, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(2000, ct);

                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var result = await _submissionApiClient.GetResultAsync(submissionId);

                    if (result is not null)
                    {
                        CurrentResult = result;
                        IsPolling = false;
                        OnResultReceived?.Invoke(result);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    IsPolling = false;
                    OnError?.Invoke(ex.Message);
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            StopPolling();
            await ValueTask.CompletedTask;
        }
    }
}
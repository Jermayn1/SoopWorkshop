using SoopWorkshop.Frontend.Services.HttpClients;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Frontend.Services.StateManagement
{
    // Fragt alle 2 Sekunden nach dem Auswertungsstatus einer Submission
    public class SubmissionPollingState : IAsyncDisposable
    {
        private const int IntervalMilliseconds = 2000;

        // Nach etwa fuenf Minuten wird abgebrochen. Ohne Obergrenze dreht sich die
        // Seite endlos, falls der Status wider Erwarten nie einen Endzustand erreicht.
        private const int MaxAttempts = 150;

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
            var attempt = 0;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(IntervalMilliseconds, ct);

                if (ct.IsCancellationRequested)
                    break;

                if (++attempt > MaxAttempts)
                {
                    Fail("Die Auswertung dauert ungewoehnlich lange. Bitte lade die Seite neu oder reiche erneut ein.");
                    break;
                }

                try
                {
                    var status = await _submissionApiClient.GetStatusAsync(submissionId);

                    if (status is null)
                    {
                        Fail("Der Auswertungsstand konnte nicht abgerufen werden. Ist die Abgabe noch vorhanden?");
                        break;
                    }

                    if (status.Status == SubmissionStatus.Failed)
                    {
                        Fail(string.IsNullOrWhiteSpace(status.ErrorMessage)
                            ? "Die Auswertung ist fehlgeschlagen."
                            : status.ErrorMessage);
                        break;
                    }

                    if (status.Status != SubmissionStatus.Done)
                        continue;

                    var result = await _submissionApiClient.GetResultAsync(submissionId);

                    if (result is null)
                    {
                        Fail("Die Auswertung ist abgeschlossen, aber es liegt kein Ergebnis vor.");
                        break;
                    }

                    CurrentResult = result;
                    IsPolling = false;
                    OnResultReceived?.Invoke(result);
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Fail(ex.Message);
                    break;
                }
            }
        }

        private void Fail(string message)
        {
            IsPolling = false;
            OnError?.Invoke(message);
        }

        public async ValueTask DisposeAsync()
        {
            StopPolling();
            await ValueTask.CompletedTask;
        }
    }
}

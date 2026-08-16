using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using Microsoft.AspNetCore.Components.Forms;

namespace SoopWorkshop.Frontend.Services.HttpClients
{
    // Kapselt die API-Calls für Submission und Auswertungsergebnisse
    public class SubmissionApiClient
    {
        private const string GenericUploadError =
            "Die Abgabe konnte nicht angenommen werden. Bitte versuche es erneut.";

        private const string ApiUnreachableError =
            "Der Server ist gerade nicht erreichbar. Bitte versuche es in einem Moment erneut.";

        private readonly HttpClient _httpClient;
        private readonly ILogger<SubmissionApiClient> _logger;

        public SubmissionApiClient(HttpClient httpClient, ILogger<SubmissionApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Läd ein/mehre .java Dateien für eine Aufgabe hoch und gibt die erstelle Submission zurück
        public async Task<ApiResult<SubmissionDto>> SubmitAsync(Guid taskItemId, IEnumerable<IBrowserFile> files)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(taskItemId.ToString()), "taskItemId");

                foreach (var file in files)
                {
                    var stream = file.OpenReadStream(SubmissionUploadLimits.MaxFileSizeBytes);
                    content.Add(new StreamContent(stream), "files", file.Name);
                }

                var response = await _httpClient.PostAsync("api/submissions", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Abgabe fuer Aufgabe {TaskItemId} abgelehnt, die API antwortete mit {StatusCode}.",
                        taskItemId, (int)response.StatusCode);

                    return ApiResult<SubmissionDto>.Failure(await ReadErrorMessageAsync(response));
                }

                var submission = await response.Content.ReadFromJsonAsync<SubmissionDto>();

                return submission is null
                    ? ApiResult<SubmissionDto>.Failure(GenericUploadError)
                    : ApiResult<SubmissionDto>.Success(submission);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception,
                    "Die API ist beim Abgeben zu Aufgabe {TaskItemId} nicht erreichbar.", taskItemId);

                return ApiResult<SubmissionDto>.Failure(ApiUnreachableError);
            }
        }

        // Fragt den Auswertungsstand ab.
        // null bedeutet: die Abgabe ist unbekannt oder die API nicht erreichbar.
        public async Task<SubmissionStatusDto?> GetStatusAsync(Guid submissionId)
        {
            var response = await _httpClient.GetAsync($"api/submissions/{submissionId}/status");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SubmissionStatusDto>();
        }

        // Ruft das Auswertungsergebnis einer Submission ab
        // null wenn Auswertung noch am laufen ist
        public async Task<EvaluationResultDto?> GetResultAsync(Guid submissionId)
        {
            var response = await _httpClient.GetAsync($"api/submissions/{submissionId}/result");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<EvaluationResultDto>();
        }

        /// <summary>
        /// Holt die Begruendung aus der Antwort, sofern sie fuer Teilnehmer taugt.
        /// </summary>
        /// <remarks>
        /// Der <c>SubmissionUploadValidator</c> antwortet mit einem deutschen Klartextsatz -
        /// genau der soll ankommen. Alles andere (JSON-Fehlerobjekt aus der
        /// ExceptionMiddleware, leerer Body) bekommt einen eigenen Satz, damit nie
        /// Rohtechnik in der Oberflaeche landet.
        /// </remarks>
        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            // Die Groessengrenze schlaegt im Server zu, bevor ein Controller laeuft -
            // dort gibt es keine Meldung, die wir weiterreichen koennten.
            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                var megabytes = SubmissionUploadLimits.MaxTotalSizeBytes / (1024 * 1024);
                return $"Die Dateien sind zusammen zu gross. Erlaubt sind hoechstens {megabytes} MB.";
            }

            var body = (await response.Content.ReadAsStringAsync()).Trim();

            if (body.Length == 0 || body.StartsWith('{') || body.StartsWith('['))
                return GenericUploadError;

            return body;
        }
    }
}

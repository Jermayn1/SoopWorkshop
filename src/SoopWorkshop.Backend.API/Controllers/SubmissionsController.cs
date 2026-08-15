using System.Text;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.API.Validation;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Shared.Constants;

namespace SoopWorkshop.Backend.API.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Nimmt alle (ein oder mehrere .java Dateien für die Aufgabe entgegen und startet die Auswertung
        [HttpPost]
        [RequestSizeLimit(SubmissionUploadLimits.MaxTotalSizeBytes)]
        public async Task<IActionResult> Create([FromForm] Guid taskItemId, [FromForm] List<IFormFile> files)
        {
            var errors = SubmissionUploadValidator.Validate(files);
            if (errors.Count > 0)
                return BadRequest(string.Join(" ", errors));

            var fileContents = new List<(string FileName, string Content)>();

            foreach (var file in files)
            {
                // Zeichensatz fest auf UTF-8, sonst haengt das Ergebnis von der
                // Systemeinstellung des Servers ab — und Umlaute zerlegen sich.
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var content = await reader.ReadToEndAsync(HttpContext.RequestAborted);
                fileContents.Add((file.FileName, content));
            }

            var result = await _submissionService.CreateAsync(taskItemId, fileContents, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Gibt den Auswertungsstand zurueck. Anders als /result unterscheidet dieser
        // Endpunkt zwischen "laeuft noch", "fehlgeschlagen" und "nicht gefunden" —
        // sonst kann das Frontend einen Fehlschlag nicht erkennen und pollt endlos.
        [HttpGet("{id:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid id)
        {
            var result = await _submissionService.GetStatusAsync(id, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        // Gibt das Ausgabeergebnis einer Aubgabe zurück
        // Während der Auswertung, wird ein Fehler zurück gegeben
        [HttpGet("{id:guid}/result")]
        public async Task<IActionResult> GetResult(Guid id)
        {
            var result = await _submissionService.GetResultAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }
    }
}

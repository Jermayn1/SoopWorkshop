using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;

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
        public async Task<IActionResult> Create([FromForm] Guid taskItemId, [FromForm] List<IFormFile> files)
        {
            if (files.Count == 0)
                return BadRequest("Es wurde keine Datei hochgeladen.");

            var fileContents = new List<(string FileName, string Content)>();

            foreach (var file in files)
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync();
                fileContents.Add((file.FileName, content));
            }

            var result = await _submissionService.CreateAsync(taskItemId, fileContents, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
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
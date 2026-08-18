using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Submissions.Interfaces;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // Uebersicht ueber die abgegebenen Loesungen. Der Nachzuegler aus Phase 5:
    // beim Pflegen der Aufgaben half sie nicht (dafuer gibt es den Probelauf),
    // waehrend des Workshops ist sie der einzige Weg zu sehen, wo die
    // Teilnehmer stehen.
    [ApiController]
    [Authorize]
    [Route("api/admin/submissions")]
    public class AdminSubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public AdminSubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // Seitenweise, neueste zuerst. Filter sind optional.
        //
        // Die Zeilen tragen keine Auswertungsdetails - dafuer verlinkt das
        // Panel auf /abgaben/{id}, also auf dieselbe Ergebnisanzeige, die der
        // Teilnehmer sieht. Eine zweite, nachgebaute Anzeige liefe beim ersten
        // Umbau auseinander (dieselbe Entscheidung wie bei der Vorschau in 5.5).
        [HttpGet]
        [ProducesResponseType<SubmissionPageDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SubmissionPageDto>> GetPage(
            [FromQuery] Guid? taskItemId,
            [FromQuery] SubmissionStatus? status,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 25)
        {
            var result = await _submissionService.GetPageAsync(
                taskItemId, status, skip, take, HttpContext.RequestAborted);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }
    }
}

using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Antwort von GET /api/submissions/{id}/status.
    // Bewusst getrennt vom Ergebnis: das Frontend muss "laeuft noch",
    // "fehlgeschlagen" und "nicht gefunden" unterscheiden koennen.
    public class SubmissionStatusDto
    {
        public Guid Id { get; set; }

        // Zu welcher Aufgabe die Abgabe gehoert. Das Frontend braucht das fuer den
        // Zurueck-Link von der Ergebnisseite — ohne dieses Feld muesste es die
        // Aufgabe erst ueber einen zweiten Aufruf ermitteln oder sie sich merken,
        // und ein direkt aufgerufener Ergebnis-Link haette gar keinen Weg zurueck.
        public Guid TaskItemId { get; set; }

        public SubmissionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }

        // Nur bei Status Failed gefuellt.
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

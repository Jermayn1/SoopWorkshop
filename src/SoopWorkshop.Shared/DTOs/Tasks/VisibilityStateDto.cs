namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Antwort der Sichtbarkeits-Endpunkte (PATCH .../visibility).
    //
    // Vorher stand hier ein anonymer Typ (new { isVisible = ... }). Fuer ein Frontend
    // in .NET war das gleichgueltig, ein erzeugter API-Vertrag kann daraus aber kein
    // Schema ableiten — die Antwort waere im Frontend als "unbekannt" angekommen.
    public class VisibilityStateDto
    {
        public bool IsVisible { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskTestDto
    {
        [Required]
        public Guid TaskItemId { get; set; }
        
        public string Input { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Die erwartete Ausgabe ist erforderlich.")]
        public string ExpectedOutput { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        [MaxLength(2000, ErrorMessage = "Die Beschreibung darf maximal 2000 Zeichen lang sein")]
        public string Description { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
    }
}
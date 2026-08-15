using System.ComponentModel.DataAnnotations;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskItemDto
    {
        [Required]
        public Guid TaskCategoryId { get; set; }

        [Required(ErrorMessage = "Der Titel ist erforderlich.")]
        [MaxLength(200, ErrorMessage = "Der Titel darf maximal 200 Zeichen lang sein.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        public string Description { get; set; } = string.Empty;
        
        public Difficulty Difficulty { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
        
        public List<string> Hints { get; set; } = [];
    }
}
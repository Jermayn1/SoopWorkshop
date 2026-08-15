using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskCategoryDto
    {
        [Required(ErrorMessage = "Der Name ist erforderlich.")]
        [MaxLength(100, ErrorMessage = "Der Name darf nicht länger als 100 Zeichen lang sein.")]
        
        public string Name { get; set; } = string.Empty;
        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        
        public int Order { get; set; }
    }
}
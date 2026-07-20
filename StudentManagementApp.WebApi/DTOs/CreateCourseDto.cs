using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class CreateCourseDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, 120, ErrorMessage = "Duration must be positive.")]
        public int DurationMonths { get; set; }

        [Range(0, 999999, ErrorMessage = "Base Rate cannot be negative.")]
        public decimal FeeAmount { get; set; }
    }
}
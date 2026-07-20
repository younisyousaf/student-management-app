using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class FeeDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Student ID is required.")]
        public int StudentId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Course ID is required.")]
        public int CourseId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal AmountPaid { get; set; }

        public string? Remarks { get; set; }
    }
}
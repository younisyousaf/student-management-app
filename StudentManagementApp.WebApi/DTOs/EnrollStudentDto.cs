using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class EnrollStudentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Student ID is required.")]
        public int StudentId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Course ID is required.")]
        public int CourseId { get; set; }
    }
}
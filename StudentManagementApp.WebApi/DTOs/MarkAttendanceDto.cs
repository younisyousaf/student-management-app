using StudentManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class MarkAttendanceDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Student ID is required.")]
        public int StudentId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Course ID is required.")]
        public int CourseId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public string? Remarks { get; set; }
    }
}

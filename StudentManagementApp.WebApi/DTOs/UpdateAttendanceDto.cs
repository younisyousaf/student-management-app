using StudentManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class UpdateAttendanceDto
    {
        [Required]
        public AttendanceStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}

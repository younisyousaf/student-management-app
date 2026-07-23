using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class EditAttendanceViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Please select a status.")]
        public AttendanceStatus Status { get; set; }

        public string? Remarks { get; set; }
    }
}
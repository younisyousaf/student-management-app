using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class MarkAttendanceViewModel
    {
        [Required(ErrorMessage = "Roll Number is required.")]
        [Display(Name = "Student Roll Number")]
        public string RollNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a course.")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a status.")]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [Display(Name = "Remarks (optional)")]
        public string? Remarks { get; set; }

        public List<Course>? AvailableCourses { get; set; }
    }
}
using StudentManagement.Core.Enums;
using StudentManagement.Core.Models;

namespace StudentManagementApp.MVC.ViewModels
{
    public class AttendanceRowViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}
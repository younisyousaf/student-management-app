using System;

namespace StudentManagementApp.MVC.ViewModels
{
    public class EnrollmentIndexViewModel
    {
        public int Id { get; set; }
        public string RollNumber { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public decimal CourseFee { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
        public string Status { get; set; } = null!;
        public DateTime EnrollDate { get; set; }
    }
}
// Fallback namespace to satisfy any lingering cached auto-generated files
namespace StudentManagementApp.MVC.Models
{
    public class EnrollmentIndexViewModel : StudentManagementApp.MVC.ViewModels.EnrollmentIndexViewModel
    {
    }
}
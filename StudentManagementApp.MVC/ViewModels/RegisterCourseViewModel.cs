using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class RegisterCourseViewModel
    {
        [Required(ErrorMessage = "Course Code is required.")]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course Name is required.")]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duration in months is required.")]
        [Range(1, 48, ErrorMessage = "Duration must be between 1 and 48 months.")]
        [Display(Name = "Duration (Months)")]
        public int DurationMonths { get; set; } = 3;

        [Required(ErrorMessage = "Fee Amount is required.")]
        [Range(0.00, 100000.00, ErrorMessage = "Fee Amount cannot be negative.")]
        [Display(Name = "Fee Amount ($)")]
        public decimal FeeAmount { get; set; } = 0.00m;
    }
}
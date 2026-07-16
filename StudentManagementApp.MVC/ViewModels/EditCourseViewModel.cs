using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class EditCourseViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = null!;

        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = null!;

        [Display(Name = "Description (Optional)")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 48, ErrorMessage = "Duration must be between 1 and 48 months.")]
        [Display(Name = "Duration (Months)")]
        public int DurationMonths { get; set; }

        [Required]
        [Range(0.00, 100000.00, ErrorMessage = "Fee amount cannot be negative.")]
        [Display(Name = "Fee Amount ($)")]
        public decimal FeeAmount { get; set; }
    }
}
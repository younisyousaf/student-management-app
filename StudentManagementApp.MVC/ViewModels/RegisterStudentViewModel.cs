using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class RegisterStudentViewModel
    {
        [Required(ErrorMessage = "Roll Number is required.")]
        [Display(Name = "Roll Number")]
        public string RollNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-15); // Sensible default
    }
}
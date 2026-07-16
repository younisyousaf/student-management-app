using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.MVC.ViewModels
{
    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Roll Number")]
        public string RollNumber { get; set; } = null!;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }
    }
}
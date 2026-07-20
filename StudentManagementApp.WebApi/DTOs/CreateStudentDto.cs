using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.WebApi.DTOs
{
    public class CreateStudentDto
    {
        [Required]
        public string RollNumber { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
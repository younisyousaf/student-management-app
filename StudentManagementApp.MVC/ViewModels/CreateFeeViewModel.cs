using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementApp.MVC.ViewModels
{
    public class CreateFeeViewModel
    {
        [Required(ErrorMessage = "Please select a student.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Please select a course.")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Amount paid must be greater than zero.")]
        [Display(Name = "Amount Paid ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Display(Name = "Remarks / Notes")]
        public string? Remarks { get; set; }

        // Dropdown lists populated from DB
        public List<SelectListItem> Students { get; set; } = new();
        public List<SelectListItem> Courses { get; set; } = new();
    }
}
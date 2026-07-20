using System;

namespace StudentManagementApp.WPF.DTOs
{
    public class StudentModel
    {
        public int Id { get; set; }
        public string RollNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class CourseModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal FeeAmount { get; set; }
    }

    public class EnrollmentVm
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime EnrollDate { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;

        // Navigation properties (if your API returns nested EF Core objects)
        public StudentModel? Student { get; set; }
        public CourseModel? Course { get; set; }
    }

    public class FeeVm
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Remarks { get; set; } = string.Empty;

        // Financial details from the API
        public decimal AmountDue { get; set; }
        public decimal RemainingBalance { get; set; }
        public bool IsFullySettled { get; set; }
        public int Status { get; set; }

        // Matches the nested objects in your JSON payload
        public StudentModel? Student { get; set; }
        public CourseModel? Course { get; set; }

        // FIXED: Clean read-only properties mapping directly to the nested objects for your XAML bindings
        public string StudentName
        {
            get => Student != null ? $"{Student.FirstName} {Student.LastName}".Trim() : string.Empty;
        }

        public string CourseCode
        {
            get => Course != null ? Course.Code : string.Empty;
        }
    }

    public class EnrollStudentDto
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
    }

    public class CreateStudentDto
    {
        public string RollNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class CreateCourseDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal FeeAmount { get; set; }
    }

    public class FeeDto
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal AmountPaid { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
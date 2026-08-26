using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record StudentSummary(int Id, string RollNumber, string FullName, string Email, string? Phone, string? Address, DateTime? DateOfBirth, DateTime? AdmissionDate);

public record StudentLookupResult(bool Found, StudentSummary? Student, string? Message);

public class StudentTools
{
    private readonly IStudentService _studentService;

    public StudentTools(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [Description("Get a single student's details by their exact roll number, e.g. 'ST-104'. " +
        "Always check the Found field first — if Found is false, no student with that roll number exists. " +
        "Do not substitute a different student.")]
    public StudentLookupResult GetStudentByRollNumber(
        [Description("The student's roll number")] string rollNumber)
    {
        var student = _studentService.GetStudentByRollNumber(rollNumber);
        return student == null
            ? new StudentLookupResult(false, null, $"No student exists with roll number '{rollNumber}'.")
            : new StudentLookupResult(true, ToSummary(student), null);
    }

    [Description("Search for students by full or partial name match. " +
        "Use this when the user gives a name instead of a roll number. " +
        "Returns an empty list if no student matches — that means no such student exists, not that the search failed.")]
    public IEnumerable<StudentSummary> SearchStudentsByName(
        [Description("Full or partial name to search for")] string name)
    {
        return _studentService.GetAllStudents()
            .Where(s => s.FullName.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Select(ToSummary);
    }

    [Description(
    "Find a student by their exact internal student ID. " +
    "Always check Found first. If Found is false, do not substitute another student.")]
    public StudentLookupResult GetStudentById(
    [Description("The exact internal student ID.")]
    int studentId)
    {
        var student = _studentService.GetStudentById(studentId);

        return student == null
            ? new StudentLookupResult(
                false,
                null,
                $"No student exists with ID '{studentId}'.")
            : new StudentLookupResult(
                true,
                ToSummary(student),
                null);
    }

    [Description(
    "Create a new student record. " +
    "All required student information must come from the user. " +
    "Never invent a roll number, name, email, or date of birth. " +
    "This modifies student data and requires human approval.")]
    public string CreateStudent(
    [Description("The student's unique roll number.")]
    string rollNumber,

    [Description("The student's first name.")]
    string firstName,

    [Description("The student's last name.")]
    string lastName,

    [Description("The student's email address.")]
    string email,

    [Description(
        "The student's date of birth.")]
    DateTime dateOfBirth,

    [Description(
        "Optional phone number.")]
    string? phone = null,

    [Description(
        "Optional address.")]
    string? address = null)
    {
        var student = new Student(rollNumber, firstName, lastName, email, dateOfBirth);

        if (phone is not null || address is not null)
        {
            student.UpdateProfile(
                firstName,
                lastName,
                phone,
                address);
        }
        _studentService.RegisterStudent(student);
        return
            $"Student '{student.FullName}' " +
            $"with roll number '{student.RollNumber}' " +
            $"was successfully created.";
    }


    [Description(
    "Update one or more fields of an existing student's profile. " +
    "Before using this tool, verify the exact student using GetStudentById. " +
    "Only explicitly supplied fields are changed. " +
    "This modifies student data and requires human approval.")]
    public string UpdateStudentProfile(
    [Description("The exact internal student ID.")]
    int studentId,

    [Description("New first name, or null to keep the current value.")]
    string? firstName = null,

    [Description("New last name, or null to keep the current value.")]
    string? lastName = null,

    [Description("New phone number, or null to keep the current value.")]
    string? phone = null,

    [Description("New address, or null to keep the current value.")]
    string? address = null,

    [Description("New email address, or null to keep the current value.")]
    string? email = null)
    {
        var student = _studentService.GetStudentById(studentId);

        if (student is null)
        {
            return $"Student with ID {studentId} was not found.";
        }

        _studentService.UpdateStudentProfile(
            studentId,
            firstName ?? student.FirstName,
            lastName ?? student.LastName,
            phone ?? student.Phone,
            address ?? student.Address,
            email ?? student.Email);

        return $"Student ID {studentId} was successfully updated.";
    }

    [Description(
    "Permanently remove an existing student from the system. " +
    "Before using this tool, verify the exact student using GetStudentById. " +
    "Never substitute another student if the requested student does not exist. " +
    "This is a destructive operation and requires human approval.")]
    public string RemoveStudent(
    [Description("The exact internal student ID.")]
    int studentId)
    {
        var student = _studentService.GetStudentById(studentId);

        if (student is null)
        {
            return $"Student with ID {studentId} was not found. No student was removed.";
        }

        _studentService.RemoveStudent(studentId);

        return $"Student ID {studentId} was successfully removed.";
    }

    private static StudentSummary ToSummary(Student student) =>
        new(student.Id, student.RollNumber, student.FullName, student.Email, student.Phone, student.Address, student.DateOfBirth, student.AdmissionDate);
}
using System.ComponentModel;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;

namespace StudentManagement.AI.Tools;

public record StudentSummary(int Id, string RollNumber, string FullName, string Email, string? Phone);

public class StudentTools
{
    private readonly IStudentService _studentService;

    public StudentTools(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [Description("Get a single student's details by their exact roll number, e.g. 'ST-104'.")]
    public StudentSummary? GetStudentByRollNumber(
        [Description("The student's roll number")] string rollNumber)
    {
        var student = _studentService.GetStudentByRollNumber(rollNumber);
        return student == null ? null : ToSummary(student);
    }

    [Description("Search for students by full or partial name match. Use this when the user gives a name instead of a roll number.")]
    public IEnumerable<StudentSummary> SearchStudentsByName(
        [Description("Full or partial name to search for")] string name)
    {
        return _studentService.GetAllStudents()
            .Where(s => s.FullName.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Select(ToSummary);
    }

    private static StudentSummary ToSummary(Student student) =>
        new(student.Id, student.RollNumber, student.FullName, student.Email, student.Phone);
}
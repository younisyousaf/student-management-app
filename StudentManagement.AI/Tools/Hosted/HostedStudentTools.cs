using System.ComponentModel;

namespace StudentManagement.AI.Tools.Hosted;

public sealed class HostedStudentTools
{
    private readonly ScopedToolExecutor _executor;

    public HostedStudentTools(
        ScopedToolExecutor executor)
    {
        _executor = executor;
    }

    [Description(
        "Get a single student's details by their exact roll number. " +
        "Always check the Found field first. " +
        "Do not substitute a different student.")]
    public StudentLookupResult GetStudentByRollNumber(
        [Description("The student's roll number.")]
        string rollNumber)
    {
        return _executor.Execute<
            StudentTools,
            StudentLookupResult>(
                tools =>
                    tools.GetStudentByRollNumber(
                        rollNumber));
    }

    [Description(
        "Search for students by full or partial name match. " +
        "Returns an empty list when no students match.")]
    public IReadOnlyList<StudentSummary> SearchStudentsByName(
        [Description("Full or partial student name.")]
        string name)
    {
        return _executor.Execute<
            StudentTools,
            IReadOnlyList<StudentSummary>>(
                tools =>
                    tools.SearchStudentsByName(name)
                        .ToList());
    }

    [Description(
        "Find a student by their exact internal student ID. " +
        "Always check Found first.")]
    public StudentLookupResult GetStudentById(
        [Description("The exact internal student ID.")]
        int studentId)
    {
        return _executor.Execute<
            StudentTools,
            StudentLookupResult>(
                tools =>
                    tools.GetStudentById(
                        studentId));
    }

    [Description(
    "Create a new student record. " +
    "All required student information must come from the user. " +
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

    [Description("The student's date of birth.")]
    DateTime dateOfBirth,

    [Description("Optional phone number.")]
    string? phone = null,

    [Description("Optional address.")]
    string? address = null)
    {
        return _executor.Execute<
            StudentTools,
            string>(
                tools =>
                    tools.CreateStudent(
                        rollNumber,
                        firstName,
                        lastName,
                        email,
                        dateOfBirth,
                        phone,
                        address));
    }

    [Description(
        "Update one or more fields of an existing student's profile. " +
        "The exact student must be verified first. " +
        "This modifies student data and requires human approval.")]
    public string UpdateStudentProfile(
        [Description("The exact internal student ID.")]
        int studentId,

        [Description(
            "New first name, or null to preserve the current value.")]
        string? firstName = null,

        [Description(
            "New last name, or null to preserve the current value.")]
        string? lastName = null,

        [Description(
            "New phone number, or null to preserve the current value.")]
        string? phone = null,

        [Description(
            "New address, or null to preserve the current value.")]
        string? address = null,

        [Description(
            "New email address, or null to preserve the current value.")]
        string? email = null)
    {
        return _executor.Execute<
            StudentTools,
            string>(
                tools =>
                    tools.UpdateStudentProfile(
                        studentId,
                        firstName,
                        lastName,
                        phone,
                        address,
                        email));
    }

    [Description(
        "Permanently remove an existing student. " +
        "The exact student must be verified first. " +
        "This is destructive and requires human approval.")]
    public string RemoveStudent(
        [Description("The exact internal student ID.")]
        int studentId)
    {
        return _executor.Execute<
            StudentTools,
            string>(
                tools =>
                    tools.RemoveStudent(
                        studentId));
    }
}

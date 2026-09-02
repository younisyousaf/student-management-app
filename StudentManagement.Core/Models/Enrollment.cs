namespace StudentManagement.Core.Models;

public class Enrollment : BaseEntity
{
    public int SchoolId { get; private set; }
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public DateTime EnrollDate { get; init; }
    public string Status { get; private set; }

    public Enrollment(int studentId, int courseId)
    {
        if (studentId <= 0 || courseId <= 0) throw new ArgumentException("Identifiers must point to authentic items.");

        StudentId = studentId;
        CourseId = courseId;
        EnrollDate = DateTime.UtcNow;
        Status = "Active";
    }

    protected Enrollment() { }

    public void TerminateEnrollment() => Status = "Dropped";

    public void GraduateEnrollment() => Status = "Completed";

    public void AssignToSchool(int schoolId)
    {
        if (schoolId <= 0)
            throw new ArgumentException("Valid school ID is required.");

        if (SchoolId != 0 && SchoolId != schoolId)
            throw new InvalidOperationException(
                "Enrollment is already assigned to another school.");

        SchoolId = schoolId;
    }

    public override string ToString() => $"Enrollment #{Id} | Student ID: {StudentId} -> Course: {CourseId} State: [{Status}]";
}
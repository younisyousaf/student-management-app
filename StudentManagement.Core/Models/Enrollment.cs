namespace StudentManagement.Core.Models;

public class Enrollment : BaseEntity
{
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

    public override string ToString() => $"Enrollment #{Id} | Student ID: {StudentId} -> Course: {CourseId} State: [{Status}]";
}
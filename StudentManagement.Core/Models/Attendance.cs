using StudentManagement.Core.Enums;

namespace StudentManagement.Core.Models;

public class Attendance : BaseEntity
{
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public DateTime Date { get; init; }
    public AttendanceStatus Status { get; private set; }
    public string? Remarks { get; private set; }

    public Attendance(int studentId, int courseId, DateTime date, AttendanceStatus status, string? remarks = null)
    {
        if (studentId <= 0 || courseId <= 0)
            throw new ArgumentException("Identifiers must point to authentic items.");
        if (date.Date > DateTime.UtcNow.Date)
            throw new ArgumentException("Cannot record attendance for a future date.");

        StudentId = studentId;
        CourseId = courseId;
        Date = date.Date;
        Status = status;
        Remarks = remarks;
    }

    protected Attendance() { }

    public void UpdateStatus(AttendanceStatus status, string? remarks = null)
    {
        Status = status;
        Remarks = remarks;
    }

    public override string ToString() =>
        $"Attendance #{Id} | Student: {StudentId} Course: {CourseId} Date: {Date:yyyy-MM-dd} Status: [{Status}]";
}
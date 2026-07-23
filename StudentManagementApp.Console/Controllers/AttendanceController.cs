using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementSystem.Helpers;

namespace StudentManagementSystem.Controllers;

public class AttendanceController
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;

    public AttendanceController(IAttendanceService attendanceService, IStudentService studentService, ICourseService courseService)
    {
        _attendanceService = attendanceService;
        _studentService = studentService;
        _courseService = courseService;
    }

    public void ManageAttendance()
    {
        string[] menuOptions = { "Mark Daily Attendance", "View Attendance History for a Student", "Update an Attendance Record" };

        while (true)
        {
            int choice = ConsoleHelper.ShowMenu("Daily Attendance Management", menuOptions);
            if (choice == 0) break;

            switch (choice)
            {
                case 1: MarkAttendance(); break;
                case 2: ViewStudentHistory(); break;
                case 3: UpdateAttendanceRecord(); break;
            }
            ConsoleHelper.Pause();
        }
    }

    private void MarkAttendance()
    {
        ConsoleHelper.Info("\n--- Mark Daily Attendance ---");

        string roll = ConsoleHelper.ReadRequired("Enter Student Roll Number");
        var student = _studentService.GetStudentByRollNumber(roll);
        if (student == null) { ConsoleHelper.Error("Student not found."); return; }

        int courseId = ConsoleHelper.ReadInt("Enter Course Id", 1);
        var course = _courseService.GetCourseById(courseId);
        if (course == null) { ConsoleHelper.Error("Course not found."); return; }

        DateTime date = ConsoleHelper.ReadDate("Enter Attendance Date");

        string[] statusOptions = { "Present", "Absent", "Late", "Excused" };
        int statusChoice = ConsoleHelper.ShowMenu("Select Status", statusOptions);
        if (statusChoice == 0) return;
        var status = (AttendanceStatus)(statusChoice - 1);

        string remarks = ConsoleHelper.ReadOptional("Remarks (optional)", "");

        try
        {
            _attendanceService.MarkAttendance(student.Id, course.Id, date, status, string.IsNullOrWhiteSpace(remarks) ? null : remarks);
            ConsoleHelper.Success($"Attendance marked: {student.FullName} - {status} on {date:yyyy-MM-dd}.");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void ViewStudentHistory()
    {
        string roll = ConsoleHelper.ReadRequired("Enter Student Roll Number");
        var student = _studentService.GetStudentByRollNumber(roll);
        if (student == null) { ConsoleHelper.Error("Student not found."); return; }

        var records = _attendanceService.GetAttendanceForStudent(student.Id);
        ConsoleHelper.PrintList($"Attendance History - {student.FullName}", records);
    }

    private void UpdateAttendanceRecord()
    {
        int attendanceId = ConsoleHelper.ReadInt("Enter Attendance Record Id", 1);

        string[] statusOptions = { "Present", "Absent", "Late", "Excused" };
        int statusChoice = ConsoleHelper.ShowMenu("Select New Status", statusOptions);
        if (statusChoice == 0) return;
        var status = (AttendanceStatus)(statusChoice - 1);

        string remarks = ConsoleHelper.ReadOptional("Remarks (optional)", "");

        try
        {
            _attendanceService.UpdateAttendance(attendanceId, status, string.IsNullOrWhiteSpace(remarks) ? null : remarks);
            ConsoleHelper.Success("Attendance record updated.");
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }
}
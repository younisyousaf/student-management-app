using StudentManagementSystem.Helpers;
using StudentManagement.Core.Interfaces;

namespace StudentManagementSystem.Controllers;

public class EnrollmentController
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IFeeService _feeService;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        IStudentService studentService,
        ICourseService courseService,
        IFeeService feeService)
    {
        _enrollmentService = enrollmentService;
        _studentService = studentService;
        _courseService = courseService;
        _feeService = feeService;
    }

    public void ManageEnrollmentsAndFees()
    {
        string[] options = { "Enroll Student in Course", "View All Active Enrollments", "Collect Student Tuition Fee (Payment)", "Check Student Fee Ledger Balance Statement" };

        while (true)
        {
            int choice = ConsoleHelper.ShowMenu("Enrollments & Academic Billings", options);
            if (choice == 0) break;

            switch (choice)
            {
                case 1: ExecuteEnrollment(); break;
                case 2: ViewEnrollments(); break;
                case 3: PostPayment(); break;
                case 4: GenerateBalanceSheet(); break;
            }
            ConsoleHelper.Pause();
        }
    }

    private void ExecuteEnrollment()
    {
        ConsoleHelper.Info("\n--- Academic Enrollment Registration Process ---");
        string roll = ConsoleHelper.ReadRequired("Enter Student Roll Number");
        var student = _studentService.GetStudentByRollNumber(roll);
        if (student == null) { ConsoleHelper.Error("Student Registry missing validation check."); return; }

        string courseCode = ConsoleHelper.ReadRequired("Enter Target Course Code");
        var course = _courseService.GetCourseByCode(courseCode);
        if (course == null) { ConsoleHelper.Error("Course target lookup missing."); return; }

        try
        {
            _enrollmentService.EnrollStudent(student.Id, course.Id);
            ConsoleHelper.Success($"Student '{student.FullName}' cleanly cataloged into '{course.Name}'.");
            ConsoleHelper.Info($"Invoice Auto-Generated: Due Balance tracking total initialized to ${course.FeeAmount:F2}");
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void ViewEnrollments()
    {
        var data = _enrollmentService.GetAllEnrollments();
        ConsoleHelper.PrintList("Master Academic Registrations Registry", data);
    }

    private void PostPayment()
    {
        ConsoleHelper.Info("\n--- Process Tuition Fee Ledger Payment Entry ---");
        string roll = ConsoleHelper.ReadRequired("Enter Student Roll Number");
        var student = _studentService.GetStudentByRollNumber(roll);
        if (student == null) { ConsoleHelper.Error("Student identity verified unrecorded."); return; }

        string code = ConsoleHelper.ReadRequired("Enter Enrolled Course Code");
        var course = _courseService.GetCourseByCode(code);
        if (course == null) { ConsoleHelper.Error("Course lookup target broken."); return; }

        var statement = _feeService.GetFeeStatement(student.Id, course.Id);
        if (statement == null)
        {
            ConsoleHelper.Error("No billing registration parameters found correlating these items.");
            return;
        }

        ConsoleHelper.Info($"\nInvoice Summary: Total Owed: ${statement.AmountDue:F2} | Paid to Date: ${statement.AmountPaid:F2} | Net Remaining Balance: ${statement.RemainingBalance:F2}");

        if (statement.IsFullySettled)
        {
            ConsoleHelper.Success("This academic transactional ledger line is already fully settled.");
            return;
        }

        decimal paymentAmount = ConsoleHelper.ReadDecimal("Enter Payment Allocation Amount", min: 0.01m);
        string remarks = ConsoleHelper.ReadRequired("Enter Memo / Remarks Notes");

        try
        {
            _feeService.ProcessStudentPayment(student.Id, course.Id, paymentAmount, remarks);
            ConsoleHelper.Success("Financial statement transaction updated. Balance deducted successfully.");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void GenerateBalanceSheet()
    {
        var records = _feeService.GetAllFeeLedgers();
        ConsoleHelper.PrintList("Global Corporate School Management Ledger Billing Statements", records);
    }
}
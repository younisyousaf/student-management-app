using StudentManagementSystem.Controllers;
using StudentManagement.Infrastructure.Helpers;
using StudentManagementSystem.Helpers;
using StudentManagement.Infrastructure.Repositories;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Services;

namespace StudentManagementSystem;

internal class Program
{
    private static void Main(string[] args)
    {
        ConsoleHelper.Info("Initializing School Management Infrastructure Engine...");

        try
        {
            // 1. Instantiate your custom DbContext configuration class
            var dbContext = new StudentManagement.Infrastructure.Data.DbContext();

            // 2. Initialize your ADO.NET SqlHelper by passing the context directly!
            var sqlHelper = new SqlHelper(dbContext);

            // 4. Pass the sqlHelper instance into the repositories cleanly
            IStudentRepository studentRepository = new StudentRepository(sqlHelper);
            ICourseRepository courseRepository = new CourseRepository(sqlHelper);
            IEnrollmentRepository enrollmentRepository = new EnrollmentRepository(sqlHelper);
            IFeeRepository feeRepository = new FeeRepository(sqlHelper);
            IAttendanceRepository attendanceRepository = new AttendanceRepository(sqlHelper);

            // 5. Services Assembly (Business Logic Layer)
            IStudentService studentService = new StudentService(studentRepository);
            ICourseService courseService = new CourseService(courseRepository);
            IFeeService feeService = new FeeService(feeRepository);
            IEnrollmentService enrollmentService = new EnrollmentService(
                enrollmentRepository,
                studentRepository,
                courseRepository,
                feeRepository
            );
            IAttendanceService attendanceService = new AttendanceService(attendanceRepository, enrollmentRepository);

            // 6. Controllers Assembly (Presentation Orchestration Layer)
            var studentController = new StudentController(studentService);
            var courseController = new CourseController(courseService);
            var enrollmentController = new EnrollmentController(enrollmentService, studentService, courseService, feeService);
            var attendanceController = new AttendanceController(attendanceService, studentService, courseService);

            // 7. Launch Root Application Loop
            RunRootApplicationLoop(studentController, courseController, enrollmentController, attendanceController);
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Fatal Application Startup Failure: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            ConsoleHelper.Pause();
        }
    }

    private static void RunRootApplicationLoop(
        StudentController studentCtrl,
        CourseController courseCtrl,
        EnrollmentController enrollmentCtrl,
        AttendanceController attendanceCtrl
        )
    {
        string[] mainMenuOptions = {
            "Student Core Directory Management",
            "Academic Course Curriculum Catalog",
            "Registrations & Tuition Financial Billings",
            "Daily Attendance Management"
        };

        while (true)
        {
            int choice = ConsoleHelper.ShowMenu("Main System Control Center", mainMenuOptions);

            if (choice == 0)
            {
                if (ConsoleHelper.Confirm("Are you completely finished with this operational session?"))
                {
                    ConsoleHelper.Success("\nSession terminated safely. Clearing active execution streams. Goodbye!");
                    break;
                }
                continue;
            }

            switch (choice)
            {
                case 1: studentCtrl.ManageStudents(); break;
                case 2: courseCtrl.ManageCourses(); break;
                case 3: enrollmentCtrl.ManageEnrollmentsAndFees(); break;
                case 4: attendanceCtrl.ManageAttendance(); break;
            }
        }
    }
}
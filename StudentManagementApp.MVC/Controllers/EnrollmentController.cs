using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.MVC.Models;
using StudentManagementApp.MVC.ViewModels;
using System;
using System.Linq;
using EnrollmentIndexViewModel = StudentManagementApp.MVC.Models.EnrollmentIndexViewModel;

namespace StudentManagementApp.MVC.Controllers
{
    public class EnrollmentController : Controller
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

        // GET: /Enrollment
        public IActionResult Index(string search, int? page)
        {
            ViewData["CurrentFilter"] = search;

            var enrollments = _enrollmentService.GetAllEnrollments();
            var students = _studentService.GetAllStudents().ToDictionary(s => s.Id);
            var courses = _courseService.GetAllCourses().ToDictionary(c => c.Id);
            var fees = _feeService.GetAllFeeLedgers();

            var allViewModels = enrollments.Select(e =>
            {
                students.TryGetValue(e.StudentId, out var student);
                courses.TryGetValue(e.CourseId, out var course);

                var feeRecord = fees.FirstOrDefault(f => f.StudentId == e.StudentId && f.CourseId == e.CourseId);

                return new EnrollmentIndexViewModel
                {
                    Id = e.Id,
                    RollNumber = student?.RollNumber ?? "N/A",
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown Student",
                    CourseName = course?.Name ?? "Unknown Course",
                    CourseFee = course?.FeeAmount ?? 0m,
                    AmountPaid = feeRecord?.AmountPaid ?? 0m,
                    AmountDue = (course?.FeeAmount ?? 0m) - (feeRecord?.AmountPaid ?? 0m),
                    EnrollDate = e.EnrollDate,
                    Status = e.Status
                };
            }).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                allViewModels = allViewModels.Where(v =>
                    v.RollNumber.ToLower().Contains(search) ||
                    v.StudentName.ToLower().Contains(search) ||
                    v.CourseName.ToLower().Contains(search));
            }

            int pageSize = 10;
            int pageIndex = page ?? 1;

            var paginatedList = PaginatedList<EnrollmentIndexViewModel>.Create(
                allViewModels,
                pageIndex,
                pageSize
            );

            return View(paginatedList);
        }

        // GET: /Enrollment/Create
        [HttpGet]
        public IActionResult Create()
        {
          //Initialize and pass the ViewModel
            var model = new CreateEnrollmentViewModel();
            return View(model);
        }

        // POST: /Enrollment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateEnrollmentViewModel model)
        {
            // validation
            if (!ModelState.IsValid)
            {
                return View(model); // Returns validation errors
            }

            try
            {
                // Call enrollment service using the properties from the validated view model
                _enrollmentService.EnrollStudent(model.StudentId, model.CourseId);

                TempData["SuccessMessage"] = "Student successfully enrolled!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Enrollment failed: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Enrollment/Details/5
        [HttpGet]
        public IActionResult Details(int id)
        {
            var enrollment = _enrollmentService.GetEnrollmentById(id);
            if (enrollment == null)
            {
                return NotFound();
            }

            var student = _studentService.GetStudentById(enrollment.StudentId);
            var course = _courseService.GetCourseById(enrollment.CourseId);
            var feeRecord = _feeService.GetAllFeeLedgers()
                .FirstOrDefault(f => f.StudentId == enrollment.StudentId && f.CourseId == enrollment.CourseId);

            var viewModel = new EnrollmentIndexViewModel
            {
                Id = enrollment.Id,
                RollNumber = student?.RollNumber ?? "N/A",
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                CourseName = course?.Name ?? "Unknown",
                CourseFee = course?.FeeAmount ?? 0m,
                AmountPaid = feeRecord?.AmountPaid ?? 0m,
                AmountDue = (course?.FeeAmount ?? 0m) - (feeRecord?.AmountPaid ?? 0m),
                EnrollDate = enrollment.EnrollDate,
                Status = enrollment.Status
            };

            return View(viewModel);
        }

        // POST: /Enrollment/Drop/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Drop(int id)
        {
            try
            {
                _enrollmentService.DropCourse(id);
                TempData["SuccessMessage"] = "Student has dropped the course.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Could not drop course: {ex.Message}";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Enrollment/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(int id)
        {
            try
            {
                _enrollmentService.CompleteCourse(id);
                TempData["SuccessMessage"] = "Course marked as completed successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Could not complete course: {ex.Message}";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
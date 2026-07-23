using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagementApp.MVC.ViewModels;
using System.Linq;

namespace StudentManagementApp.MVC.Controllers
{
    public class AttendanceController : Controller
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

        public IActionResult Index(string? rollNumber)
        {

            var students = _studentService.GetAllStudents().ToDictionary(s => s.Id);
            //Console.WriteLine("Students", students);
            var courses = _courseService.GetAllCourses().ToDictionary(c => c.Id);

            var attendanceRecords = _attendanceService.GetAllAttendance();

            if (!string.IsNullOrWhiteSpace(rollNumber))
            {
                var student = _studentService.GetStudentByRollNumber(rollNumber);
                if (student == null)
                {
                    ViewData["ErrorMessage"] = $"No student found with roll number '{rollNumber}'.";
                    return View(Enumerable.Empty<AttendanceRowViewModel>());
                }
                attendanceRecords = attendanceRecords.Where(a => a.StudentId == student.Id);
            }

            var records = attendanceRecords
                .OrderByDescending(a => a.Date)
                .Select(a => new AttendanceRowViewModel
                {
                    Id = a.Id,
                    StudentName = students.TryGetValue(a.StudentId, out var student) ? student.FullName : $"Student #{a.StudentId}",
                    CourseName = courses.TryGetValue(a.CourseId, out var course) ? course.Name : $"Course #{a.CourseId}",
                    Date = a.Date,
                    Status = a.Status,
                    Remarks = a.Remarks
                });

            return View(records);
        }

        // GET: /Attendance/Mark
        [HttpGet]
        public IActionResult Mark()
        {
            return View(new MarkAttendanceViewModel { AvailableCourses = _courseService.GetAllCourses().ToList() });
        }

        // POST: /Attendance/Mark
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Mark(MarkAttendanceViewModel model)
        {
            model.AvailableCourses = _courseService.GetAllCourses().ToList();

            if (!ModelState.IsValid)
                return View(model);

            var student = _studentService.GetStudentByRollNumber(model.RollNumber);
            if (student == null)
            {
                ModelState.AddModelError(nameof(model.RollNumber), "No student found with this roll number.");
                return View(model);
            }

            try
            {
                _attendanceService.MarkAttendance(student.Id, model.CourseId, model.Date, model.Status, model.Remarks);
                TempData["SuccessMessage"] = $"Attendance marked for {student.FullName}.";
                return RedirectToAction(nameof(Index), new { rollNumber = model.RollNumber });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Attendance/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var attendance = _attendanceService.GetAttendanceById(id);
            if (attendance == null) return NotFound();

            var student = _studentService.GetStudentById(attendance.StudentId);
            var course = _courseService.GetCourseById(attendance.CourseId);

            return View(new EditAttendanceViewModel
            {
                Id = attendance.Id,
                StudentName = student?.FullName ?? $"Student #{attendance.StudentId}",
                CourseName = course?.Name ?? $"Course #{attendance.CourseId}",
                Date = attendance.Date,
                Status = attendance.Status,
                Remarks = attendance.Remarks
            });
        }

        // POST: /Attendance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, EditAttendanceViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            try
            {
                _attendanceService.UpdateAttendance(model.Id, model.Status, model.Remarks);
                TempData["SuccessMessage"] = "Attendance record updated.";

                var attendance = _attendanceService.GetAttendanceById(model.Id);
                var student = attendance != null ? _studentService.GetStudentById(attendance.StudentId) : null;
                return RedirectToAction(nameof(Index), new { rollNumber = student?.RollNumber });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
    }
}
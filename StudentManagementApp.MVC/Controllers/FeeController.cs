using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagementApp.MVC.ViewModels;
using System;
using System.Linq;

namespace StudentManagementApp.MVC.Controllers
{
    public class FeeController : Controller
    {
        private readonly IFeeService _feeService;
        private readonly IStudentService _studentService;
        private readonly ICourseService _courseService;

        public FeeController(IFeeService feeService, IStudentService studentService, ICourseService courseService)
        {
            _feeService = feeService;
            _studentService = studentService;
            _courseService = courseService;
        }

        // GET: /Fee
        public IActionResult Index()
        {
            var fees = _feeService.GetAllFeeLedgers();
            var students = _studentService.GetAllStudents();
            var studentDict = students.ToDictionary(s => s.Id);

            var viewModelList = fees.Select(f => new FeeIndexViewModel
            {
                Id = f.Id,
                StudentName = studentDict.TryGetValue(f.StudentId, out var student)
                    ? student.FullName
                    : $"Unknown (ID: {f.StudentId})",
                AmountDue = f.AmountDue,
                AmountPaid = f.AmountPaid,
                // FIX: Use .GetValueOrDefault() to convert DateTime? to DateTime cleanly
                PaymentDate = f.PaymentDate.GetValueOrDefault(DateTime.MinValue)
            }).ToList();

            return View(viewModelList);
        }

        // GET: /Fee/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateFeeViewModel();
            // Dropdowns can remain empty, Select2 handles population via AJAX
            return View(model);
        }

        // POST: /Fee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateFeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Invoke service to map and execute transaction state logic
                _feeService.ProcessStudentPayment(
                    model.StudentId,
                    model.CourseId,
                    model.Amount,
                    model.Remarks
                );

                TempData["SuccessMessage"] = "Payment successfully posted to ledger!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Fee/Details/5
        [HttpGet]
        public IActionResult Details(int id)
        {
            // Retrieve the fee ledger from the service layer
            var fee = _feeService.GetFeeById(id);
            if (fee == null)
            {
                return NotFound();
            }

            // Ensure student and course navigation properties are loaded via their respective services
            if (fee.Student == null)
                fee.Student = _studentService.GetStudentById(fee.StudentId);

            if (fee.Course == null)
                fee.Course = _courseService.GetCourseById(fee.CourseId);

            return View(fee);
        }

        [HttpGet]
        public IActionResult Pay(int id)
        {
            // FIX: Retrieve via service layer instead of direct DB access
            var fee = _feeService.GetFeeById(id);
            if (fee == null) return NotFound();

            // Explicitly load navigation entities if your repository doesn't already include them
            if (fee.Student == null) fee.Student = _studentService.GetStudentById(fee.StudentId);
            if (fee.Course == null) fee.Course = _courseService.GetCourseById(fee.CourseId);

            if (fee.Status == PaymentStatus.Paid)
            {
                TempData["ErrorMessage"] = "This fee ledger is already fully paid.";
                return RedirectToAction(nameof(Index));
            }

            return View(fee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pay(int id, decimal amount, string? remarks)
        {
            var fee = _feeService.GetFeeById(id);
            if (fee == null) return NotFound();

            try
            {
                // FIX: Process payment using the domain service wrapper
                _feeService.ProcessStudentPayment(fee.StudentId, fee.CourseId, amount, remarks);

                TempData["SuccessMessage"] = "Payment successfully captured!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to process payment: {ex.Message}");

                // Reload navigations for view rendering
                if (fee.Student == null) fee.Student = _studentService.GetStudentById(fee.StudentId);
                if (fee.Course == null) fee.Course = _courseService.GetCourseById(fee.CourseId);
                return View(fee);
            }
        }

        private void PopulateDropdowns(CreateFeeViewModel model)
        {
            model.Students = _studentService.GetAllStudents()
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.FullName} ({s.RollNumber})"
                }).ToList();

            model.Courses = _courseService.GetAllCourses()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} ({c.Code})"
                }).ToList();
        }
    }
}
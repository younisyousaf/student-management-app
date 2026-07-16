using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.MVC.Models;
using StudentManagementApp.MVC.ViewModels;
using System;
using System.Linq;

namespace StudentManagementApp.MVC.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        // GET: /Student
        public IActionResult Index(string search, int? page)
        {
            ViewData["CurrentFilter"] = search;

            // Fetch the active list of students and cast as queryable
            var query = _studentService.GetAllStudents().AsQueryable();

            // Apply Search Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(s =>
                    s.RollNumber.ToLower().Contains(search) ||
                    s.FirstName.ToLower().Contains(search) ||
                    s.LastName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search));
            }

            // Implement Pagination (e.g., 10 records per page)
            int pageSize = 10;
            int pageIndex = page ?? 1;

            var paginatedStudents = PaginatedList<Student>.Create(query, pageIndex, pageSize);

            return View(paginatedStudents);
        }

        // GET: /Student/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RegisterStudentViewModel());
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var student = new Student(
                    model.RollNumber,
                    model.FirstName,
                    model.LastName,
                    model.Email,
                    model.DateOfBirth
                );

                _studentService.RegisterStudent(student);
                TempData["SuccessMessage"] = "Student registered successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Student/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var student = _studentService.GetStudentById(id);
            if (student == null)
            {
                return NotFound();
            }

            var model = new EditStudentViewModel
            {
                Id = student.Id,
                RollNumber = student.RollNumber,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                DateOfBirth = student.DateOfBirth
            };

            return View(model);
        }

        // POST: /Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, EditStudentViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Fetch the student to ensure they exist before trying to update
                var student = _studentService.GetStudentById(id);
                if (student == null)
                {
                    return NotFound();
                }
           
                _studentService.UpdateStudentProfile(
                    id: model.Id,
                    firstName: model.FirstName,
                    lastName: model.LastName,
                    phone: student.Phone,      
                    address: student.Address,  
                    email: model.Email
                );

                TempData["SuccessMessage"] = "Student details updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Student/Details/5
        [HttpGet]
        public IActionResult Details(int id)
        {
            var student = _studentService.GetStudentById(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // GET: /Student/Delete/5
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var student = _studentService.GetStudentById(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: /Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _studentService.RemoveStudent(id);
                TempData["SuccessMessage"] = "Student deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Unable to delete student: {ex.Message}";
                var student = _studentService.GetStudentById(id);
                return View("Delete", student);
            }
        }

        // AJAX search helper for Select2 integration
        [HttpGet]
        public IActionResult SearchStudents(string searchTerm)
        {
            var query = _studentService.GetAllStudents();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s => s.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                         s.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                         s.RollNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            var results = query.Take(10)
                               .Select(s => new { id = s.Id, text = $"{s.RollNumber} - {s.FirstName} {s.LastName}" })
                               .ToList();

            return Json(results);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.MVC.Models;
using StudentManagementApp.MVC.ViewModels;
using System;
using System.Linq;

namespace StudentManagementApp.MVC.Controllers
{
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // GET: /Course
        public IActionResult Index(string search, int? page)
        {
            ViewData["CurrentFilter"] = search;

            var query = _courseService.GetAllCourses().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Code.ToLower().Contains(search) ||
                    c.Name.ToLower().Contains(search) ||
                    (c.Description != null && c.Description.ToLower().Contains(search)));
            }

            int pageSize = 10;
            int pageIndex = page ?? 1;

            var paginatedCourses = PaginatedList<Course>.Create(query, pageIndex, pageSize);

            return View(paginatedCourses);
        }

        // GET: /Course/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RegisterCourseViewModel());
        }

        // POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RegisterCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var course = new Course(
                    model.CourseCode,
                    model.CourseName,
                    model.DurationMonths,
                    model.FeeAmount
                );

                _courseService.CreateCourse(course);
                TempData["SuccessMessage"] = "Course successfully created and added to the catalog!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Course/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var course = _courseService.GetCourseById(id);
            if (course == null)
            {
                return NotFound();
            }

            var model = new EditCourseViewModel
            {
                Id = course.Id,
                CourseCode = course.Code,
                CourseName = course.Name,
                Description = course.Description,
                DurationMonths = course.DurationMonths,
                FeeAmount = course.FeeAmount
            };

            return View(model);
        }

        // POST: /Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, EditCourseViewModel model)
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
                // Verify target course exists
                var course = _courseService.GetCourseById(id);
                if (course == null)
                {
                    return NotFound();
                }

                // 1. Update Core Details (Name, Description, Duration)
                _courseService.UpdateCourseDetails(model.Id, model.CourseName, model.Description, model.DurationMonths);

                // 2. Update Pricing if it has modified
                if (course.FeeAmount != model.FeeAmount)
                {
                    _courseService.UpdateCoursePricing(model.Id, model.FeeAmount);
                }

                TempData["SuccessMessage"] = "Course updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: /Course/Details/5
        [HttpGet]
        public IActionResult Details(int id)
        {
            var course = _courseService.GetCourseById(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // GET: /Course/Delete/5
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var course = _courseService.GetCourseById(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: /Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _courseService.RemoveCourse(id);
                TempData["SuccessMessage"] = "Course removed from catalog successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Unable to delete course: {ex.Message}";
                var course = _courseService.GetCourseById(id);
                return View("Delete", course);
            }
        }

        [HttpGet]
        public IActionResult SearchCourses(string searchTerm)
        {
            var query = _courseService.GetAllCourses();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                         c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            var results = query.Take(10)
                               .Select(c => new { id = c.Id, text = $"{c.Code} - {c.Name} (${c.FeeAmount})" })
                               .ToList();

            return Json(results);
        }
    }
}
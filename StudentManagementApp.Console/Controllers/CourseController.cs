using StudentManagementSystem.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagementSystem.Controllers;

public class CourseController
{
    private readonly ICourseService _courseService;

    public CourseController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public void ManageCourses()
    {
        string[] options = { "Create New Course", "View All Courses", "Update Course Base Rate (Fee)" };

        while (true)
        {
            int choice = ConsoleHelper.ShowMenu("Course Management", options);
            if (choice == 0) break;

            switch (choice)
            {
                case 1: CreateCourse(); break;
                case 2: ViewAllCourses(); break;
                case 3: UpdatePricing(); break;
            }
            ConsoleHelper.Pause();
        }
    }

    private void CreateCourse()
    {
        ConsoleHelper.Info("\n--- Create New Course Catalog entry ---");
        string code = ConsoleHelper.ReadRequired("Enter Unique Course Code");
        string name = ConsoleHelper.ReadRequired("Enter Course Title Name");
        string? description = ConsoleHelper.ReadRequired("Enter Description");
        int duration = ConsoleHelper.ReadInt("Enter Duration (in Months)", min: 1, max: 48);
        decimal fee = ConsoleHelper.ReadDecimal("Enter Tuition Base Fee Amount", min: 0.00m);

        try
        {
            var course = new Course(code, name, duration, fee) { Description = description };
            _courseService.CreateCourse(course);
            ConsoleHelper.Success("New Academic Course added to the operational catalog.");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void ViewAllCourses()
    {
        var courses = _courseService.GetAllCourses();
        ConsoleHelper.PrintList("Academic Course Catalog", courses);
    }

    private void UpdatePricing()
    {
        string code = ConsoleHelper.ReadRequired("Enter Course Code to re-price");
        var course = _courseService.GetCourseByCode(code);

        if (course == null)
        {
            ConsoleHelper.Error("Course mismatch error: Code not discovered.");
            return;
        }

        ConsoleHelper.Info($"Current Fee Structure for {course.Name}: ${course.FeeAmount:F2}");
        decimal newPrice = ConsoleHelper.ReadDecimal("Enter New Tuition Rate", min: 0.00m);

        try
        {
            _courseService.UpdateCoursePricing(course.Id, newPrice);
            ConsoleHelper.Success("Course financial statements successfully updated.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }
}
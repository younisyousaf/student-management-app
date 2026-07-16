using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.Core.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    // Constructor Injection: Loose coupling via the repository abstraction layer
    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public void CreateCourse(Course course)
    {
        // Course Code must be unique across the system
        var existingCourse = _courseRepository.GetByCode(course.Code);
        if (existingCourse != null)
            throw new InvalidOperationException($"Creation rejected: Course Code '{course.Code}' is already assigned to '{existingCourse.Name}'.");

        // Enforce minimum length rules at a business level
        if (course.DurationMonths < 1)
            throw new ArgumentException("Creation rejected: A structural academic course must last at least 1 month.");

        // Validate initial fee constraints
        if (course.FeeAmount < 0)
            throw new ArgumentException("Creation rejected: Course pricing metrics cannot reflect negative values.");

        // If all business rules pass, safely commit the record to SQL Server
        _courseRepository.Add(course);
    }

    public Course? GetCourseById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid course identification key.");
        return _courseRepository.GetById(id);
    }

    public Course? GetCourseByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Course code cannot be blank.");
        return _courseRepository.GetByCode(code);
    }

    public IEnumerable<Course> GetAllCourses()
    {
        return _courseRepository.GetAll();
    }

    public void UpdateCourseDetails(int id, string name, string? description, int durationMonths)
    {
        // Verify target course existence
        var course = _courseRepository.GetById(id)
            ?? throw new KeyNotFoundException($"Update aborted: Course with ID {id} does not exist.");

        // Validate input parameters against business boundaries
        if (durationMonths < 1)
            throw new ArgumentException("Update aborted: Course duration must span at least 1 month.");

        // Mutate encapsulated domain state safely using internal method validation
        course.ModifyCourseDetails(name, durationMonths);
        course.Description = description; // Mutable property open for description adjustments

        // Save the cleanly modified rich model back to the database
        _courseRepository.Update(course);
    }

    public void UpdateCoursePricing(int id, decimal newFeeAmount)
    {
        // Fetch targeted course
        var course = _courseRepository.GetById(id)
            ?? throw new KeyNotFoundException($"Pricing run aborted: Course with ID {id} does not exist.");

        // Delegate state modification logic to the rich domain model.
        // This invokes our encapsulated logic checking that (newAmount >= 0)
        course.ApplyPricingUpdate(newFeeAmount);

        // Save the modified transaction state to SQL
        _courseRepository.Update(course);
    }

    public void RemoveCourse(int id)
    {
        _ = _courseRepository.GetById(id)
            ?? throw new KeyNotFoundException($"Deletion aborted: Course with ID {id} does not exist.");

        // Note: Relational database logic handles cascading rules for enrollments
        _courseRepository.Delete(id);
    }
}
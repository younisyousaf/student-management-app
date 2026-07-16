using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface ICourseService
{
    void CreateCourse(Course course);
    Course? GetCourseById(int id);
    Course? GetCourseByCode(string code);
    IEnumerable<Course> GetAllCourses();
    void UpdateCourseDetails(int id, string name, string? description, int durationMonths);
    void UpdateCoursePricing(int id, decimal newFeeAmount);
    void RemoveCourse(int id);
}
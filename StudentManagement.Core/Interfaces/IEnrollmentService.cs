using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IEnrollmentService
{
    void EnrollStudent(int studentId, int courseId);
    Enrollment? GetEnrollmentById(int id);
    IEnumerable<Enrollment> GetAllEnrollments();
    IEnumerable<Enrollment> GetEnrollmentsByStudent(int studentId);
    void DropCourse(int enrollmentId);
    void CompleteCourse(int enrollmentId);
}
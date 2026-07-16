using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IStudentService
{
    void RegisterStudent(Student student);
    Student? GetStudentById(int id);
    Student? GetStudentByRollNumber(string rollNumber);
    IEnumerable<Student> GetAllStudents();
    void UpdateStudentProfile(int id, string firstName, string lastName, string? phone, string? address, string email);
    void RemoveStudent(int id);
}
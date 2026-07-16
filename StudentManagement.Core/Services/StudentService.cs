using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.Core.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public void RegisterStudent(Student student)
    {
        // Email must contain '@'
        if (!student.Email.Contains('@'))
            throw new ArgumentException($"Registration rejected: '{student.Email}' is not a structurally valid email address.");

        // Roll number must be globally unique
        var existingRoll = _studentRepository.GetByRollNumber(student.RollNumber);
        if (existingRoll != null)
            throw new InvalidOperationException($"Registration rejected: Roll Number '{student.RollNumber}' is already assigned to another student.");

        // Email must be unique across the school system
        var existingEmail = _studentRepository.GetByEmail(student.Email);
        if (existingEmail != null)
            throw new InvalidOperationException($"Registration rejected: Email '{student.Email}' is already registered.");

        // If all business rules pass, safely commit the record to the database
        _studentRepository.Add(student);
    }

    public Student? GetStudentById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid student identification key.");
        return _studentRepository.GetById(id);
    }

    public Student? GetStudentByRollNumber(string rollNumber)
    {
        if (string.IsNullOrWhiteSpace(rollNumber)) throw new ArgumentException("Roll number cannot be empty.");
        return _studentRepository.GetByRollNumber(rollNumber);
    }

    public IEnumerable<Student> GetAllStudents()
    {
        return _studentRepository.GetAll();
    }

    public void UpdateStudentProfile(int id, string firstName, string lastName, string? phone, string? address, string email)
    {
        // Fetch the existing entity from the database
        var student = _studentRepository.GetById(id)
            ?? throw new KeyNotFoundException($"Update aborted: Student with ID {id} does not exist.");

        // Validate email syntax if it's being modified
        if (!email.Contains('@'))
            throw new ArgumentException($"Update aborted: '{email}' is not a structurally valid email address.");

        // Ensure the new email isn't already taken by a different student
        var existingEmailUser = _studentRepository.GetByEmail(email);
        if (existingEmailUser != null && existingEmailUser.Id != id)
            throw new InvalidOperationException($"Update aborted: Email '{email}' is already in use by another student.");

        // Update the object's internal state safely using its encapsulated domain methods
        student.UpdateProfile(firstName, lastName, phone, address);
        student.ChangeEmail(email);

        // Save the updated object back to the database
        _studentRepository.Update(student);
    }

    public void RemoveStudent(int id)
    {
        var student = _studentRepository.GetById(id)
            ?? throw new KeyNotFoundException($"Deletion aborted: Student with ID {id} does not exist.");

        _studentRepository.Delete(id);
    }
}
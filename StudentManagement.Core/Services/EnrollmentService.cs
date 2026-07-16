using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.Core.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IFeeRepository _feeRepository;

    // Orchestrating multiple repositories allows us to implement transactional compound rules cleanly
    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        IFeeRepository feeRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _feeRepository = feeRepository;
    }

    public void EnrollStudent(int studentId, int courseId)
    {
        // Ensure the Target Student actually exists in SQL
        var student = _studentRepository.GetById(studentId)
            ?? throw new KeyNotFoundException($" Student with ID {studentId} does not exist.");

        // Ensure the Target Course actually exists in SQL
        var course = _courseRepository.GetById(courseId)
            ?? throw new KeyNotFoundException($" Course with ID {courseId} does not exist.");

        // Double Enrollment Protection
        if (_enrollmentRepository.IsAlreadyEnrolled(studentId, courseId))
            throw new InvalidOperationException($"Student '{student.FullName}' is already actively enrolled in '{course.Name}'.");

        // Core Domain Execution - Create and save the Enrollment
        var enrollment = new Enrollment(studentId, courseId);
        _enrollmentRepository.Add(enrollment);

        // Automated Invoice Generation
        // We capture the real-time pricing directly from the rich Course domain model 
        // to prevent hardcoded pricing or UI variance bugs.
        var initialInvoice = new Fee(studentId, courseId, course.FeeAmount);
        _feeRepository.Add(initialInvoice);
    }

    public Enrollment? GetEnrollmentById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid enrollment identification key.");
        return _enrollmentRepository.GetById(id);
    }

    public IEnumerable<Enrollment> GetAllEnrollments()
    {
        return _enrollmentRepository.GetAll();
    }

    public IEnumerable<Enrollment> GetEnrollmentsByStudent(int studentId)
    {
        if (studentId <= 0) throw new ArgumentException("Invalid student identification key.");
        return _enrollmentRepository.GetByStudentId(studentId);
    }

    public void DropCourse(int enrollmentId)
    {
        var enrollment = _enrollmentRepository.GetById(enrollmentId)
            ?? throw new KeyNotFoundException($"Operation failed: Enrollment record #{enrollmentId} not found.");

        enrollment.TerminateEnrollment();
        _enrollmentRepository.Update(enrollment);
    }

    public void CompleteCourse(int enrollmentId)
    {
        var enrollment = _enrollmentRepository.GetById(enrollmentId)
            ?? throw new KeyNotFoundException($"Operation failed: Enrollment record #{enrollmentId} not found.");

        enrollment.GraduateEnrollment();
        _enrollmentRepository.Update(enrollment);
    }
}
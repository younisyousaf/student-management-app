using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.Core.Services;

public class FeeService : IFeeService
{
    private readonly IFeeRepository _feeRepository;

    public FeeService(IFeeRepository feeRepository)
    {
        _feeRepository = feeRepository;
    }

    public Fee? GetFeeById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid fee invoice identification key.");
        return _feeRepository.GetById(id);
    }

    public Fee? GetFeeStatement(int studentId, int courseId)
    {
        if (studentId <= 0 || courseId <= 0) throw new ArgumentException("Valid identification numbers must be provided.");
        return _feeRepository.GetByStudentAndCourse(studentId, courseId);
    }

    public IEnumerable<Fee> GetAllFeeLedgers()
    {
        return _feeRepository.GetAll();
    }

    public void ProcessStudentPayment(int studentId, int courseId, decimal amount, string? remarks)
    {
        // Payment validation must be greater than zero before processing
        if (amount <= 0)
            throw new ArgumentException("Payment processing aborted: Payment amount must be strictly greater than zero.");

        // Fetch corresponding invoice log
        var feeRecord = _feeRepository.GetByStudentAndCourse(studentId, courseId)
            ?? throw new KeyNotFoundException("Payment processing aborted: No outstanding fee invoice statement exists for this student/course tracking alignment.");

        // Delegate balance deduction checking directly to the rich domain entity.
        // This ensures the payment does not exceed the remaining balance due.
        feeRecord.ProcessPayment(amount, remarks);

        // Commit changes to SQL Server
        _feeRepository.Update(feeRecord);
    }
}
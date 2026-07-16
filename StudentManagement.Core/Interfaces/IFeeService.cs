using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IFeeService
{
    Fee? GetFeeById(int id);
    Fee? GetFeeStatement(int studentId, int courseId);
    IEnumerable<Fee> GetAllFeeLedgers();
    void ProcessStudentPayment(int studentId, int courseId, decimal amount, string? remarks);
}
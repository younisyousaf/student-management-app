using StudentManagement.Core.Enums;

namespace StudentManagement.AI.Models;

public record StudentFeeAssessment(
    int StudentId,
    int CourseId,
    decimal AmountDue,
    decimal AmountPaid,
    PaymentStatus PaymentStatus,
    string Summary,
    string Observation);
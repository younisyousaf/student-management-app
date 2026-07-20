namespace StudentManagementApp.WebApi.DTOs
{
    public class FeeResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? Remarks { get; set; }
        public int Status { get; set; }
        public decimal RemainingBalance { get; set; }
        public bool IsFullySettled { get; set; }
    }
}
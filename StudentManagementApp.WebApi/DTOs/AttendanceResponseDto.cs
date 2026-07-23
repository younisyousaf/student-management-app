namespace StudentManagementApp.WebApi.DTOs
{
    public class AttendanceResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime Date { get; set; }
        public int Status { get; set; }
        public string? Remarks { get; set; }
    }
}
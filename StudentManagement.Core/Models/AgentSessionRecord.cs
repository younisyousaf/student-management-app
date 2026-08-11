namespace StudentManagement.Core.Models
{
    public class AgentSessionRecord
    {
        public int Id { get; set; }

        public string SessionId { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string SerializedSession { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
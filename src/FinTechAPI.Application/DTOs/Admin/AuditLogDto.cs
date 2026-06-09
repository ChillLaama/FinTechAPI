namespace FinTechAPI.Application.DTOs
{
    public class AuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AuditLogQueryDto
    {
        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Limit { get; set; } = 50;
    }
}



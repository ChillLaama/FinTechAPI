namespace FinTechAPI.Application.DTOs
{
    public class SystemAlertDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>info | warning | critical</summary>
        public string Severity { get; set; } = "info";

        public bool IsDismissed { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


namespace EduPlatform.DAL.Entities;

public sealed class Message : BaseEntity
{
    public Guid ChatSessionId { get; set; }

    public ChatSession ChatSession { get; set; } = null!;

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public ICollection<RetrievalLog> RetrievalLogs { get; set; } = [];
}

namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions.Models;

public class ChatMetric
{
    public string? ChatId { get; set; }

    public long? LastMessageDate { get; set; }

    public string? LastMessageId { get; set; }

    public int UnreadCount { get; set; }
}
namespace LetsTalk.Server.Configuration.Models;

public class MessagingSettings
{
    /// <summary>Number of messages in one page. Allowed: 1 to 100.</summary>
    public int MessagesPerPage { get; set; } = 20;
}

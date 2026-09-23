using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class ChatMessageStatus
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("chatId")]
    public string? ChatId { get; set; }

    [JsonProperty("accountId")]
    public string? AccountId { get; set; }

    [JsonProperty("messageId")]
    public string? MessageId { get; set; }

    [JsonProperty("dateReadUnix")]
    public long? DateReadUnix { get; set; }
}

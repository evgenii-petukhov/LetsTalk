using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class Message
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("textHtml")]
    public string? TextHtml { get; set; }

    [JsonProperty("senderId")]
    public string? SenderId { get; set; }

    [JsonProperty("chatId")]
    public string? ChatId { get; set; }

    [JsonProperty("dateCreatedUnix")]
    public long? DateCreatedUnix { get; set; }

    [JsonProperty("image")]
    public Image? Image { get; set; }

    [JsonProperty("imagePreview")]
    public Image? ImagePreview { get; set; }

    [JsonProperty("linkPreviewId")]
    public string? LinkPreviewId { get; set; }

    [JsonProperty("linkPreview")]
    public LinkPreview? LinkPreview { get; set; }

    [JsonProperty("emojisOnly")]
    public bool EmojisOnly { get; set; }

    [JsonProperty("emojiCount")]
    public int EmojiCount { get; set; }
}

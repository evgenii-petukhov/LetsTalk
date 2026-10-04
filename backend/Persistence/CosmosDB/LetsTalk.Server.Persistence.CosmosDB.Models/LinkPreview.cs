using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class LinkPreview
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("imageUrl")]
    public string? ImageUrl { get; set; }
}

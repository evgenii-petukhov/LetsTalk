using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class Chat
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("image")]
    public Image? Image { get; set; }

    [JsonProperty("isIndividual")]
    public bool IsIndividual { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("accountIds")]
    public List<string>? AccountIds { get; set; }
}

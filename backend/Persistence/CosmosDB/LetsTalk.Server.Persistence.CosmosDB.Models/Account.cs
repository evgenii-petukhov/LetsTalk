using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class Account
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("accountTypeId")]
    public int AccountTypeId { get; set; }

    [JsonProperty("externalId")]
    public string? ExternalId { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonProperty("firstName")]
    public string? FirstName { get; set; }

    [JsonProperty("lastName")]
    public string? LastName { get; set; }

    [JsonProperty("image")]
    public Image? Image { get; set; }
}

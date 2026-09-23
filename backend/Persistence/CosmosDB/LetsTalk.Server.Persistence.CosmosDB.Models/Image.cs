using Newtonsoft.Json;

namespace LetsTalk.Server.Persistence.CosmosDB.Models;

public class Image
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("imageFormatId")]
    public int ImageFormatId { get; set; }

    [JsonProperty("width")]
    public int? Width { get; set; }

    [JsonProperty("height")]
    public int? Height { get; set; }

    [JsonProperty("fileStorageTypeId")]
    public int FileStorageTypeId { get; set; }
}

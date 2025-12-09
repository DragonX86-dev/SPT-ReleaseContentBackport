using System.Text.Json.Serialization;

namespace ReleaseContentBackport.DataGenerator.Models;

public record BarterItem
{
    [JsonPropertyName("count")]
    public required double Count { get; init; }
    
    [JsonPropertyName("item")]
    public required MongoIdContainer Item { get; init; }
}
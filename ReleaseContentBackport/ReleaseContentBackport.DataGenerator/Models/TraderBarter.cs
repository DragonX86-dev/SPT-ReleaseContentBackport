using System.Text.Json.Serialization;

namespace ReleaseContentBackport.DataGenerator.Models;

public record TraderBarter
{
    [JsonPropertyName("buyLimit")]
    public required int BuyLimit { get; init; }
    
    [JsonPropertyName("level")]
    public required int Level { get; init; }
    
    [JsonPropertyName("requiredItems")]
    public required List<BarterItem> RequiredItems { get; init; }
    
    [JsonPropertyName("rewardItems")]
    public required List<BarterItem> RewardItems { get; init; }
    
}
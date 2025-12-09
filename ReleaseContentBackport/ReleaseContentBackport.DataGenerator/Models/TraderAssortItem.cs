using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ReleaseContentBackport.DataGenerator.Models;

public record TraderAssortItem
{
    [JsonPropertyName("traderId")]
    public required MongoId TraderId { get; init; }
    
    [JsonPropertyName("item")]
    public required Item Item { get; init; }

    [JsonPropertyName("subItems")] 
    public required List<Item> SubItems { get; init; }
    
    [JsonPropertyName("barterScheme")]
    public required List<BarterScheme> BarterScheme { get; init; }
    
    [JsonPropertyName("loyaltyLevel")]
    public required int LoyaltyLevel { get; init; }
};
using System.Text.Json.Serialization;

namespace ReleaseContentBackport.DataGenerator.Models;

public record TraderCashOffer
{
    [JsonPropertyName("buyLimit")]
    public required int BuyLimit { get; init; }
    
    [JsonPropertyName("level")]
    public required int Level { get; init; }
    
    [JsonPropertyName("price")]
    public required int Price { get; init; }
    
    [JsonPropertyName("currencyItem")]
    public required MongoIdContainer CurrencyItem { get; init; }
    
    [JsonPropertyName("item")]
    public required MongoIdContainer Item { get; init; }
}

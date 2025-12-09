using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.DataGenerator.Models;

public record TraderConfig
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("normalizedName")]
    public required string NormalizedName { get; init; }
    
    [JsonPropertyName("cashOffers")]
    public required List<TraderCashOffer> CashOffers { get; init; }
    
    [JsonPropertyName("barters")]
    public required List<TraderBarter> Barters { get; init; }
}
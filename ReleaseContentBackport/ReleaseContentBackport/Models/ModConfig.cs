using System.Text.Json.Serialization;

namespace ReleaseContentBackport.Models;

public record ModConfig
{
    [JsonPropertyName("refSellsGpCoinEnable")]
    public required bool RefSellsGpCoinEnable { get; init; }
}
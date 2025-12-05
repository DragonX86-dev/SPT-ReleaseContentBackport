using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.DataGenerator.Models;

public record ItemLocale
{
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("shortName")]
    public required string ShortName { get; init; }
    
    [JsonPropertyName("description")]
    public required string Description { get; init; }
};
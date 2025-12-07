using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.Models;

public record ItemConfig
{
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("is_new")] 
    public bool IsNew { get; init; } = true;
    
    [JsonPropertyName("compatibleItems")]
    public required Dictionary<string, List<MongoId>> CompatibleItems { get; init; }
    
    [JsonPropertyName("conflictingItems")]
    public required List<MongoId> ConflictingItems { get; init; }
}
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.DataGenerator.Models;

public record ModuleItem
{
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("is_new")]
    public required bool IsNew { get; init; }
    
    [JsonPropertyName("conflictingItems")]
    public required MongoId[] ConflictingItems { get; init; }
    
    [JsonPropertyName("compatibleItems")]
    public required Dictionary<string, List<MongoId>> CompatibleItems { get; init; }
}
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.Models;

public record ModuleItem
{
    [JsonPropertyName("id")]
    public required MongoId Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    [JsonPropertyName("childs")]
    public required ModuleItem[] Childs { get; init; }
    
    [JsonPropertyName("conflictingItems")]
    public required MongoId[] ConflictingItems { get; init; }
    
    [JsonPropertyName("compatibleItems")]
    public required MongoId[] CompatibleItems { get; init; }
}
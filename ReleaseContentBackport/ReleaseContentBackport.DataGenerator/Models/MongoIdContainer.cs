using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ReleaseContentBackport.DataGenerator.Models;

public record MongoIdContainer
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
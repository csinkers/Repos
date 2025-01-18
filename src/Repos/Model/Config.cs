using System.Text.Json.Serialization;

namespace Repos.Model;

public class Config
{
    [JsonPropertyName("repos")]
    public List<string> Repos { get; } = new();
}

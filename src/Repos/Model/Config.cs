using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Repos.Model;

public class Config
{
    [JsonPropertyName("repos")]
    public ObservableCollection<string> Repos { get; } = new();
}

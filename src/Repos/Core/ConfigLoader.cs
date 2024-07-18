using System.Text.Json;
using Repos.Model;

namespace Repos.Core;

public static class ConfigLoader
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public static Config Load(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(path))
            return new Config { Repos = { @"C:\Depot\bb\Repos", @"C:\Depot\bb\ualbion" } };

        var span = File.ReadAllBytes(path);
        return JsonSerializer.Deserialize<Config>(span.AsSpan(), _options) ?? new Config();
    }

    public static void Save(Config config, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, JsonSerializer.Serialize(config, _options));
    }
}

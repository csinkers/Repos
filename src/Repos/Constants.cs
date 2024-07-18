namespace Repos;

public static class Constants
{
    public const string ConfigFilename = "config.json";
    public static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReposTool"
    );
    public static readonly string ConfigPath = Path.Combine(DataDir, ConfigFilename);
}

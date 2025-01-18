using System.Collections.ObjectModel;
using Repos.Model;

namespace Repos.Core;

public class RepoManager : IRepoManager
{
    readonly Lock _syncRoot = new();
    readonly Config _config;
    readonly Dictionary<string, RepoStatus> _db = new();

    public ObservableCollection<string> Repos { get; }

    public RepoManager()
    {
        _config = ConfigLoader.Load(Constants.ConfigPath);
        Repos = _config.Repos;
    }

    public void AddRepo(string path)
    {
        lock (_syncRoot)
        {
            if (!_config.Repos.Contains(path))
                _config.Repos.Add(path);

            SaveConfig();
        }
    }

    public void RemoveRepo(string path)
    {
        lock (_syncRoot)
        {
            _config.Repos.Remove(path);
            SaveConfig();
        }
    }

    public void RefreshRepo(string path) { }

    public void FetchRepo(string path) { }

    public void RefreshAll() { }

    public void FetchAll() { }

    public RepoStatus? GetStatus(string path)
    {
        return null;
    }

    void SaveConfig() => ConfigLoader.Save(_config, Constants.ConfigPath);
}

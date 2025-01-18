using Repos.Model;
using Repos.Ui;

namespace Repos.Core;

public class RepoManager
{
    readonly Lock _syncRoot = new();
    readonly Dictionary<string, Repo> _repos;
    TreeNode? _tree;

    public TreeNode Tree
    {
        get
        {
            lock (_repos)
                return _tree ??= TreeNode.Build(_repos.Values);
        }
    }

    public RepoManager(Config config) => _repos = config.Repos.ToDictionary(x => x, x => new Repo(x));

    void Dirty() => _tree = null;
    public void AddRepo(string path)
    {
        lock (_syncRoot)
        {
            if (!_repos.ContainsKey(path))
                _repos.Add(path, new Repo(path));

            Dirty();
            SaveConfig();
        }
    }

    public void RemoveRepo(string path)
    {
        lock (_syncRoot)
        {
            _repos.Remove(path);
            Dirty();
            SaveConfig();
        }
    }

    public async Task RefreshAll(CancellationToken ct)
    {
        var tasks = new List<Task>();
        lock (_repos)
            foreach (var repo in _repos.Values)
                tasks.Add(repo.Refresh(ct));

        await Task.WhenAll(tasks);
    }

    public async Task FetchAll(CancellationToken ct)
    {
        var tasks = new List<Task>();
        lock (_repos)
            foreach (var repo in _repos.Values)
                tasks.Add(repo.Fetch(ct));

        await Task.WhenAll(tasks);
    }

    void SaveConfig()
    {
        var config = new Config();
        config.Repos.AddRange(_repos.Values.Select(x => x.Path));
        ConfigLoader.Save(config, Constants.ConfigPath);
    }
}

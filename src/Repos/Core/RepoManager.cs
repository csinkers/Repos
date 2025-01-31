using Repos.Model;
using Repos.Ui;

namespace Repos.Core;

public class RepoManager : IDisposable
{
    readonly Lock _lock = new();
    readonly Dictionary<string, IRepo> _repos = new();
    readonly RepoFactory _repoFactory;
    TreeNode? _tree;

    public TreeNode Tree
    {
        get
        {
            lock (_lock)
                return _tree ??= TreeNode.Build(_repos.Values);
        }
    }

    public RepoManager(Config config, RepoFactory repoFactory)
    {
        _repoFactory = repoFactory ?? throw new ArgumentNullException(nameof(repoFactory));
        DoAsync(
            "Adding repos",
            () =>
            {
                foreach (var path in config.Repos)
                    AddRepoInner(path);

                return Task.CompletedTask;
            }
        );
    }

    void Dirty() => _tree = null;

    public string? BusyMessage { get; private set; }

    public void AddRepo(string path)
    {
        DoAsync(
            "Adding repo",
            () =>
            {
                {
                    AddRepoInner(path);
                    return Task.CompletedTask;
                }
            }
        );
    }

    void AddRepoInner(string path)
    {
        lock (_lock)
            if (_repos.ContainsKey(path))
                return;

        var repo = _repoFactory.Create(path);

        lock (_lock)
        {
            _repos.Add(path, repo);
            Dirty();
            SaveConfig();
        }
    }

    public void RemoveRepo(string path)
    {
        lock (_lock)
        {
            if (_repos.Remove(path, out var repo))
                repo.Dispose();

            Dirty();
            SaveConfig();
        }
    }

    public Task RefreshAll(CancellationToken ct) =>
        DoAsync(
            "Refreshing all repos",
            async () =>
            {
                var tasks = new List<Task>();
                lock (_lock)
                    foreach (var repo in _repos.Values)
                        tasks.Add(repo.Refresh(ct));

                await Task.WhenAll(tasks);
            },
            ct
        );

    public Task FetchAll(CancellationToken ct) =>
        DoAsync(
            "Refreshing all repos",
            async () =>
            {
                var tasks = new List<Task>();
                lock (_lock)
                    foreach (var repo in _repos.Values)
                        tasks.Add(repo.Fetch(ct));

                await Task.WhenAll(tasks);
            },
            ct
        );

    void SaveConfig()
    {
        var config = new Config();
        config.Repos.AddRange(_repos.Values.Select(x => x.Path));
        ConfigLoader.Save(config, Constants.ConfigPath);
    }

    Task DoAsync(string description, Func<Task> task, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (BusyMessage != null)
                return Task.CompletedTask;

            BusyMessage = description;
        }

        var result = Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    await task();
                }
                finally
                {
                    lock (_lock)
                        BusyMessage = null;
                }
            },
            ct
        );

        return result.Unwrap();
    }

    public void Dispose()
    {
        lock (_repos)
        {
            foreach (var repo in _repos.Values)
                repo.Dispose();

            _repos.Clear();
        }
    }

    public void AddReposInDirectory(string path)
    {
        DoAsync(
            "Adding repos",
            () =>
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                    if (Path.Exists(Path.Combine(dir, ".git")))
                        AddRepoInner(dir);

                return Task.CompletedTask;
            }
        );
    }
}

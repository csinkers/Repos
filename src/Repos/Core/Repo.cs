using LibGit2Sharp;

namespace Repos.Core;

public class Repo : IDisposable
{
    readonly string _path;
    readonly Repository _repo;
    RepositoryStatus _status;

    public Repo(string path)
    {
        _path = path;
        try
        {
            _repo = new Repository(path);
        }
        catch (RepositoryNotFoundException ex)
        {
            // TODO
        }

        _status = _repo?.RetrieveStatus(new StatusOptions());
    }

    public override string ToString() => Path;

    public string Path => _path;
    public string LastSync
    {
        get
        {
            var fetchHeadPath = System.IO.Path.Combine(_path, ".git", "FETCH_HEAD");
            var fileInfo = new FileInfo(fetchHeadPath);
            return DescribeUtcTime(fileInfo.LastWriteTimeUtc);
        }
    }

    static string DescribeUtcTime(DateTime time)
    {
        if (time == DateTime.MinValue)
            return "never";

        var now = DateTime.UtcNow;
        var interval = now - time;
        if (interval.TotalDays > 365)
            return $"{interval.TotalDays / 365:F1} years";

        if (interval.TotalDays > 30)
        {
            int years = now.Year - time.Year;
            int months = now.Month - time.Month;
            int totalMonths = years * 12 + months;
            return $"{totalMonths} months";
        }

        if (interval.TotalDays > 1)
            return $"{interval.TotalDays:N0} days";

        if (interval.TotalHours > 1)
            return $"{interval.TotalHours:N0} hours";

        if (interval.TotalMinutes > 1)
            return $"{interval.TotalMinutes:N0} mins";

        return $"{interval.TotalSeconds:N0} secs";
    }

    public string Branch => _repo.Head.FriendlyName;
    public int Ahead => _repo.Head.TrackingDetails.AheadBy ?? 0;
    public int Behind => _repo.Head.TrackingDetails.BehindBy ?? 0;
    public int Unstaged =>
        _status.Modified.Count() + _status.Missing.Count() + _status.Untracked.Count();
    public int Staged => _status.Staged.Count();

    public async Task Refresh(CancellationToken ct)
    {
        await Task.Run(
            () =>
            {
                _status = _repo.RetrieveStatus(new StatusOptions());
            },
            ct
        );
    }

    public async Task Fetch(CancellationToken ct)
    {
        await Task.Run(
            () =>
            {
                var remote = _repo.Network.Remotes["origin"];
                _repo.Network.Fetch(remote.Name, remote.RefSpecs.Select(x => x.Specification));
                _status = _repo.RetrieveStatus(new StatusOptions());
            },
            ct
        );
    }

    public void Dispose() => _repo.Dispose();
}

using LibGit2Sharp;

namespace Repos.Core;

public class Repo : IRepo
{
    readonly Repository _repo;

    public Repo(string path, Repository repo)
    {
        Path = path;
        _repo = repo;
        RefreshInner();
    }

    public override string ToString() => Path;

    public string Path { get; }
    public string Branch { get; private set; } = "";
    public int Ahead { get; private set; }
    public int Behind { get; private set; }
    public int Unstaged { get; private set; }
    public int Staged { get; private set; }

    public string LastSync
    {
        get
        {
            var fetchHeadPath = System.IO.Path.Combine(Path, ".git", "FETCH_HEAD");
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

    public async Task Refresh(CancellationToken ct) => await Task.Run(RefreshInner, ct);

    void RefreshInner()
    {
        var status = _repo.RetrieveStatus(new StatusOptions());
        Branch = _repo.Head.FriendlyName;
        Ahead = _repo.Head.TrackingDetails.AheadBy ?? 0;
        Behind = _repo.Head.TrackingDetails.BehindBy ?? 0;
        Unstaged = status.Modified.Count() + status.Missing.Count() + status.Untracked.Count();
        Staged = status.Staged.Count();
    }

    public async Task Fetch(CancellationToken ct)
    {
        await Task.Run(
            () =>
            {
                var remote = _repo.Network.Remotes["origin"];
                _repo.Network.Fetch(remote.Name, remote.RefSpecs.Select(x => x.Specification));
                RefreshInner();
            },
            ct
        );
    }

    public void Dispose() => _repo.Dispose();
}

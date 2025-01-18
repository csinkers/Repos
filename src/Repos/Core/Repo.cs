namespace Repos.Core;

public class Repo(string path)
{
    public override string ToString() => Path;
    public string Path => path;
    public string LastSync { get; private set; } = "3 mo";
    public string Branch { get; private set; } = "master";
    public int Ahead { get; private set; } = 2;
    public int Behind { get; private set; } = 1;
    public int Unstaged { get; private set; }
    public int Staged { get; private set; }

    public async Task Refresh(CancellationToken ct)
    {
    }

    public async Task Fetch(CancellationToken ct)
    {
    }
}

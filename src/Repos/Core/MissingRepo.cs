namespace Repos.Core;

public class MissingRepo(string path) : IRepo
{
    public void Dispose() { }

    public string Path => path;
    public string LastSync => "";
    public string Branch => "";
    public int Ahead => 0;
    public int Behind => 0;
    public int Unstaged => 0;
    public int Staged => 0;

    public Task Refresh(CancellationToken ct) => Task.CompletedTask;

    public Task Fetch(CancellationToken ct) => Task.CompletedTask;
}

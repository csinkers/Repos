namespace Repos.Core;

public interface IRepo : IDisposable
{
    string Path { get; }
    string LastSync { get; }
    string Branch { get; }
    int Ahead { get; }
    int Behind { get; }
    int Unstaged { get; }
    int Staged { get; }

    Task Refresh(CancellationToken ct);
    Task Fetch(CancellationToken ct);
}

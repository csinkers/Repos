using System.Collections.ObjectModel;
using Repos.Model;

namespace Repos;

public interface IRepoManager
{
    ObservableCollection<string> Repos { get; }
    void AddRepo(string path);
    void RemoveRepo(string path);
    void RefreshRepo(string path);
    void FetchRepo(string path);
    void RefreshAll();
    void FetchAll();
    RepoStatus? GetStatus(string path);
}

using System.Collections.ObjectModel;

namespace Repos.Model;

public record RepoStatus(
    string Path,
    DateTime LastCheck,
    string ActiveBranch,
    int Changed,
    int Staged,
    ObservableCollection<BranchStatus> Branches
);

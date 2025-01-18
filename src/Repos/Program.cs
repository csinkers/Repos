using Repos.Core;
using Repos.Ui;

namespace Repos;

public static class Program
{
    public static void Main()
    {
        IRepoManager repoManager = new RepoManager();
        var main = new MainWindow(repoManager);
        UiRenderer.Run(main.Draw);
    }
}

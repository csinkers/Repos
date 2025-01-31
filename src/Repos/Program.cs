using Repos.Core;
using Repos.Ui;

namespace Repos;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var config = ConfigLoader.Load(Constants.ConfigPath);
        var repoFactory = new RepoFactory();
        using var repoManager = new RepoManager(config, repoFactory);
        var main = new MainWindow(repoManager);
        UiRenderer.Run(main.Draw);
    }
}

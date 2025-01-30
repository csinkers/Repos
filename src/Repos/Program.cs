using Repos.Core;
using Repos.Ui;

namespace Repos;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var config = ConfigLoader.Load(Constants.ConfigPath);
        using var repoManager = new RepoManager(config);
        var main = new MainWindow(repoManager);
        UiRenderer.Run(main.Draw);
    }
}

using Repos.Core;
using Repos.Ui;

namespace Repos;

public static class Program
{
    public static void Main()
    {
        var config = ConfigLoader.Load(Constants.ConfigPath);
        using var repoManager = new RepoManager(config);
        repoManager.AddRepo(@"C:\Depot\bb\Repos");
        repoManager.AddRepo(@"C:\Depot\bb\ualbion");
        repoManager.AddRepo(@"C:\Depot\bb\ualbion\deps\SerdesNet");
        repoManager.AddRepo(@"C:\Depot\bb\ualbion\deps\superpower");
        repoManager.AddRepo(@"C:\Depot\bb\ualbion\deps\veldrid");
        var main = new MainWindow(repoManager);
        UiRenderer.Run(main.Draw);
    }
}

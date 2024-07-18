using Repos.Core;
using Repos.Ui;
using Terminal.Gui;

namespace Repos;

public static class Program
{
    public static void Main()
    {
        Application.Init();

        ConfigurationManager.Themes!.Theme = "Dark";
        ConfigurationManager.Apply();

        IRepoManager repoManager = new RepoManager();
        using (var window = new MainWindow(repoManager))
            Application.Run(window);

        Application.Shutdown();
    }
}

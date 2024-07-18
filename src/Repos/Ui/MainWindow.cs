using Terminal.Gui;

namespace Repos.Ui;

public sealed class MainWindow : Toplevel
{
    public MainWindow(IRepoManager manager)
    {
        var contentView = new ContentView(manager) { Width = Dim.Fill(), Height = Dim.Fill() - 1 };
        var helpBar = new StatusBar { };
        Add(contentView, helpBar);
    }
}

using Terminal.Gui;

namespace Repos.Ui;

public sealed class HelpBar : View
{
    public HelpBar()
    {
        Add(
            new Label
            {
                Text =
                    "?: help, a: add repo, d: remove, r: refresh, R: refresh all, f: fetch, F: fetch all",
                Width = Dim.Fill()
            }
        );
    }
}

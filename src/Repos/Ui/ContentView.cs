using Terminal.Gui;

namespace Repos.Ui;

public sealed class ContentView : View
{
    /*

    General:
    Rounded borders
    Black background
    Light gray text

    Highlights in green
    Secondary in light blue
    Status bar - light blue
    Command: <key> | Command2: <key> | ... | Keybindings: ? | Cancel: <esc>

    List view
    Columns:
    #. Name
    #. Unstaged + Staged files
    #. Current branch w/ ahead/behind
    #. Ahead/behind counts for remote branches
    #. Local branches without a remote
    #. Full path

    Ordering: Alpha, first favourites then the rest.

    Keybinds:

    <esc>: Quit
    ?: Show help
    n: New repo
    d: Remove repo
    r: Refresh repo
    F5: Refresh all
    f: Fast forward
    F: Fast forward all
    p: Pull
    P: Pull all

    +----+----+------+
    |    |    |      |
    |    |    |      |
    | 1  |  2 |  3   |
    |    |    |      |
    |    |    |      |
    +----+----+------+

    1: Repos (tree)
    2: Branch
    3: Details

    */
    public ContentView(IRepoManager manager)
    {
        var repoPane = new RepoView(manager)
        {
            Height = Dim.Fill(),
            Width = Dim.Fill(),
            BorderStyle = LineStyle.Rounded,
        };
        Add(repoPane);
/*
        var branchPane = new BranchView(repoPane)
        {
            X = Pos.Right(repoPane),
            Height = Dim.Fill(),
            Width = Dim.Percent(33),
            BorderStyle = LineStyle.Rounded,
        };

        var detailsPane = new DetailsView(branchPane)
        {
            X = Pos.Right(branchPane),
            Height = Dim.Fill(),
            Width = Dim.Fill(),
            BorderStyle = LineStyle.Rounded,
        };

        Add(repoPane, branchPane, detailsPane);
*/
    }
}

namespace Repos.Ui;

public sealed class ContentView
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
    /*
    level1 = search path
    level2 = repo dir names

    up/down - navigate tree
    left/right - switch pane
    space - toggle node expansion status
    n - add search path
    d - remove search path/repo
    i - toggle showing ignored repos
    f - fetch selected repo
    F - fetch all repos
    r - refresh selected repo
    R - refresh all repos
    */
    public ContentView(IRepoManager manager)
    {
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

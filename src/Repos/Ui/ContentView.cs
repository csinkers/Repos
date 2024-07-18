using Terminal.Gui;

namespace Repos.Ui;

public sealed class ContentView : View
{
    /*

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
            Width = Dim.Percent(33),
            BorderStyle = LineStyle.Rounded,
        };

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
    }
}

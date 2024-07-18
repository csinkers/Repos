using Terminal.Gui;

namespace Repos.Ui;

public sealed class RepoView : View
{
    readonly IRepoManager _manager;
    readonly TreeView<ModelObject> _tree;

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
    public RepoView(IRepoManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _tree = new TreeView<ModelObject> { Width = Dim.Fill(), Height = Dim.Fill(), };

        var root = new DirNode();
        foreach (var repo in manager.Repos)
        {
            var parts = repo.Split(Path.DirectorySeparatorChar);
            root.Add(parts);
        }

        _tree.TreeBuilder = new ModelObjectTreeBuilder();
        foreach (var child in root.Children)
            _tree.AddObject(child.Value);

        _tree.ExpandAll();
        Add(_tree);
    }
}

namespace Repos.Ui;

public class RepoNode : ModelObject
{
    public DirNode? Parent { get; }
    public string? Name { get; }
    public string? FullPath =>
        Name == null
            ? null
            : Parent?.FullPath == null
                ? Name
                : Path.Combine(Parent.FullPath, Name);

    public RepoNode(string name, DirNode parent)
    {
        Name = name;
        Parent = parent;
    }

    public override string ToString() => Name ?? "";
}

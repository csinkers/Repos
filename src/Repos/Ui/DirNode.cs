using System.Text;

namespace Repos.Ui;

public class DirNode : ModelObject
{
    public DirNode() { }

    public DirNode(string name, DirNode parent)
    {
        Name = name;
        Parent = parent;
    }

    public DirNode? Parent { get; }
    public Dictionary<string, ModelObject> Children { get; } = new();
    public List<ModelObject> DisplayChildren { get; } = new();
    public string? Name { get; }
    public string? DisplayName { get; private set; }
    public string? FullPath =>
        Name == null
            ? null
            : Parent?.FullPath == null
                ? Name
                : Path.Combine(Parent.FullPath, Name);

    public override string ToString() => DisplayName ?? "";

    void Rebuild()
    {
        var sb = new StringBuilder();
        sb.Append(Name);
        DisplayChildren.Clear();

        DirNode node = this;
        for (; ; )
        {
            if (node.Children.Count != 1)
                break;

            var child = node.Children.Single().Value;
            if (child is not DirNode childDir)
                break;

            sb.Append(Path.DirectorySeparatorChar);
            sb.Append(childDir.Name);
            node = childDir;
        }

        foreach (var child in node.Children.Values)
            DisplayChildren.Add(child);

        DisplayName = sb.ToString();
    }

    public void Add(ReadOnlySpan<string> parts)
    {
        if (parts.Length == 0)
            return;

        var head = parts[0];
        if (!Children.TryGetValue(head, out var child))
        {
            if (parts.Length == 1)
                child = new RepoNode(head, this);
            else
                child = new DirNode(head, this);

            Children[head] = child;
        }

        if (child is DirNode dirChild)
            dirChild.Add(parts[1..]);

        Rebuild();
    }
}

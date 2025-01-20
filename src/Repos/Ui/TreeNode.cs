using ImGuiNET;
using Repos.Core;

namespace Repos.Ui;

public abstract record TreeNode
{
    protected abstract Repo? DrawNode(); // Returns selected node (if any)

    public static TreeNode Build(IEnumerable<Repo> repos)
    {
        var root = new DirNode("");
        foreach (var repo in repos)
        {
            var parts = repo.Path.Split(Path.DirectorySeparatorChar);
            var node = root;

            for (var index = 0; index < parts.Length; index++)
            {
                string part = parts[index];

                if (index == parts.Length - 1)
                {
                    node.Children.Add(new RepoNode(part, repo));
                    break;
                }

                var child = node.Children.OfType<DirNode>().FirstOrDefault(x => x.Name == part);
                if (child == null)
                {
                    child = new DirNode(part);
                    node.Children.Add(child);
                }

                node = child;
            }
        }

        root = DirNode.ConsumeSingleItemChildren(root);
        return root;
    }

    public static Repo? DrawTree(TreeNode tree)
    {
        float textBaseWidth = ImGui.CalcTextSize("A").X;
        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.BordersV |
            ImGuiTableFlags.BordersOuterH |
            ImGuiTableFlags.Resizable |
            // ImGuiTableFlags.RowBg |
            ImGuiTableFlags.NoBordersInBody;

        if (!ImGui.BeginTable("tbl", 7, tableFlags))
            return null;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide);
        ImGui.TableSetupColumn("Last Fetched", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12.0f);
        ImGui.TableSetupColumn("Branch", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12.0f);
        ImGui.TableSetupColumn("Ahead", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 8.0f);
        ImGui.TableSetupColumn("Behind", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 8.0f);
        ImGui.TableSetupColumn("Unstaged", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 8.0f);
        ImGui.TableSetupColumn("Staged", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 8.0f);
        ImGui.TableHeadersRow();

        var result = tree.DrawNode();

        ImGui.EndTable();
        return result;
    }

    record RepoNode(string Name, Repo Repo) : TreeNode
    {
        public override string ToString() => Repo.Path;
        protected override Repo? DrawNode()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.TreeNodeEx(Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen);
            var result = ImGui.IsItemHovered() ? Repo : null;
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.LastSync);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.Branch);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.Ahead.ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.Behind.ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.Unstaged.ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted(Repo.Staged.ToString());

            return result;
        }
    }

    record DirNode(string Name, params List<TreeNode> Children) : TreeNode
    {
        public override string ToString() => Name;
        protected override Repo? DrawNode()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.SetNextItemOpen(true, ImGuiCond.FirstUseEver);
            bool open = ImGui.TreeNodeEx(Name);
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();

            Repo? result = null;
            if (open)
            {
                foreach (var child in Children)
                {
                    var temp = child.DrawNode();
                    result ??= temp;
                }

                ImGui.TreePop();
            }

            return result;
        }

        public static DirNode ConsumeSingleItemChildren(DirNode node)
        {
            while (node.Children is [DirNode child])
            {
                var name = string.IsNullOrEmpty(node.Name)
                        ? child.Name
                        : $"{node.Name}{Path.DirectorySeparatorChar}{child.Name}";

                node = new DirNode(name) { Children = child.Children };
            }

            for (var index = 0; index < node.Children.Count; index++)
            {
                var child = node.Children[index];
                child = child switch
                {
                    DirNode dir => ConsumeSingleItemChildren(dir),
                    _ => child
                };

                node.Children[index] = child;
            }

            return node;
        }
    }
}
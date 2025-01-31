using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Repos.Core;

namespace Repos.Ui;

public abstract record TreeNode
{
    protected abstract IRepo? DrawNode(); // Returns selected node (if any)

    public static TreeNode Build(IEnumerable<IRepo> repos)
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

    public static IRepo? DrawTree(TreeNode tree)
    {
        float textBaseWidth = ImGui.CalcTextSize("A").X;
        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.BordersV
            | ImGuiTableFlags.BordersOuterH
            | ImGuiTableFlags.Resizable
            |
            // ImGuiTableFlags.RowBg |
            ImGuiTableFlags.NoBordersInBody;

        if (!ImGui.BeginTable("tbl", 7, tableFlags))
            return null;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide);
        ImGui.TableSetupColumn(
            "Last Fetched",
            ImGuiTableColumnFlags.WidthFixed,
            textBaseWidth * 12.0f
        );
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

    record RepoNode(string Name, IRepo Repo) : TreeNode
    {
        public override string ToString() => Repo.Path;

        protected override IRepo? DrawNode()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.PushStyleColor(ImGuiCol.Text, GetColor());
            ImGui.TreeNodeEx(
                Name,
                ImGuiTreeNodeFlags.Leaf
                    | ImGuiTreeNodeFlags.Bullet
                    | ImGuiTreeNodeFlags.NoTreePushOnOpen
            );

            if (ImGui.IsItemClicked())
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lazygit",
                    Arguments = $"-p \"{Repo.Path}\""
                };

                Process.Start(startInfo);
            }

            var result = ImGui.IsItemHovered() ? Repo : null;
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.LastSync);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.Branch);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.Ahead.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.Behind.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.Unstaged.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(Repo.Staged.ToString());
            ImGui.PopStyleColor();

            return result;
        }

        Vector4 GetColor()
        {
            if (Repo.Unstaged > 0 || Repo.Staged > 0)
                return new Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Red

            if (Repo.Behind > 0)
                return new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange

            if (Repo.Ahead > 0)
                return new Vector4(0.0f, 1.0f, 0.0f, 1.0f); // Green

            if (IsLastFetchTooOld())
                return new Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Grey

            return new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
        }

        bool IsLastFetchTooOld() =>
            DateTime.TryParse(Repo.LastSync, out var lastSync)
            && (DateTime.Now - lastSync).TotalDays > 7;
    }

    record DirNode(string Name, params List<TreeNode> Children) : TreeNode
    {
        public override string ToString() => Name;

        protected override IRepo? DrawNode()
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

            IRepo? result = null;
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

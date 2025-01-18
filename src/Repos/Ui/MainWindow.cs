using ImGuiNET;

namespace Repos.Ui;

public class MainWindow(IRepoManager repoManager)
{
    public void Draw()
    {
        RenderTree();
    }

    static readonly List<MyTreeNode> Nodes =
    [
        new("Root with Long Name", "Folder",  -1,
            new("Music", "Folder",  -1,
                new("File1_a.wav", "Audio file", 123000),
                new("File1_b.wav", "Audio file", 456000)
            ),
            new("Textures", "Folder",  -1,
                new("Image001.png", "Image file", 203128),
                new("Copy of Image001.png", "Image file", 203256),
                new("Copy of Image001 (Final2).png", "Image file", 203512)
            ),
            new("desktop.ini", "System file", 1024)
        )
    ];

    class MyTreeNode(string name, string type, int size, params List<MyTreeNode> children)
    {
        public string Name { get; } = name;
        public string Type { get; } = type;
        public int Size { get; } = size;
        public readonly List<MyTreeNode> Children = children;

        public void DisplayNode(bool isRoot)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (Children.Count > 0)
            {
                bool open = ImGui.TreeNodeEx(Name, isRoot ? ImGuiTreeNodeFlags.SpanAllColumns : 0);
                if (!isRoot)
                {
                    ImGui.TableNextColumn();
                    ImGui.TextDisabled("--");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Type);
                }

                if (!open)
                    return;

                foreach (var child in Children)
                    child.DisplayNode(false);

                ImGui.TreePop();
            }
            else
            {
                ImGui.TreeNodeEx(Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                ImGui.TableNextColumn(); ImGui.Text(Size.ToString());
                ImGui.TableNextColumn(); ImGui.TextUnformatted(Type);
            }
        }
    }

    const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.BordersV |
        ImGuiTableFlags.BordersOuterH |
        ImGuiTableFlags.Resizable |
        ImGuiTableFlags.RowBg |
        ImGuiTableFlags.NoBordersInBody;

    void RenderTree()
    {
        float textBaseWidth = ImGui.CalcTextSize("A").X;
        if (!ImGui.BeginTable("3ways", 3, TableFlags))
            return;

        // The first column will use the default _WidthStretch when ScrollX is Off and _WidthFixed when ScrollX is On
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12.0f);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 18.0f);
        ImGui.TableHeadersRow();

        Nodes[0].DisplayNode(true);

        ImGui.EndTable();
    }
}
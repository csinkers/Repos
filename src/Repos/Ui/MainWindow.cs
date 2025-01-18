using ImGuiNET;
using Repos.Core;

namespace Repos.Ui;

public class MainWindow(RepoManager repoManager)
{
    public void Draw()
    {
        if (ImGui.Button("Refresh All"))
            Task.Run(() => repoManager.RefreshAll(CancellationToken.None));

        ImGui.SameLine();

        if (ImGui.Button("Fetch All"))
            Task.Run(() => repoManager.FetchAll(CancellationToken.None));

        TreeNode.DrawTree(repoManager.Tree);
    }
}
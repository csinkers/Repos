using ImGuiNET;
using Repos.Core;
using Veldrid;

namespace Repos.Ui;

public class MainWindow(RepoManager repoManager)
{
    public void Draw(Input input)
    {
        ImGui.BeginDisabled(repoManager.BusyMessage != null);

        if (ImGui.Button("Add Repo (Ctrl+A)") || input.Ctrl(Key.A))
        {
            var dir = DirectoryDialog.Open("Select a repository", @"C:\");
            if (dir != null && Directory.Exists(dir))
                repoManager.AddRepo(dir);
        }

        ImGui.SameLine();
        if (ImGui.Button("Add All Repos in Directory (Ctrl+Shift+A)") || input.ShiftCtrl(Key.A))
        {
            var dir = DirectoryDialog.Open("Select directory containing repos", @"C:\");
            if (dir != null && Directory.Exists(dir))
                repoManager.AddReposInDirectory(dir);
        }

        if (ImGui.Button("Refresh All (Ctrl+Shift+R)") || input.ShiftCtrl(Key.R))
            Task.Run(() => repoManager.RefreshAll(CancellationToken.None));

        ImGui.SameLine();

        if (ImGui.Button("Fetch All (Ctrl+Shift+F)") || input.ShiftCtrl(Key.F))
            Task.Run(() => repoManager.FetchAll(CancellationToken.None));

        ImGui.TextUnformatted(repoManager.BusyMessage ?? "");
        ImGui.EndDisabled();

        IRepo? selected = TreeNode.DrawTree(repoManager.Tree);

        if (selected != null)
        {
            ImGui.TextUnformatted(selected.Path);
            if (ImGui.Button("Refresh (Ctrl+R)") || input.Ctrl(Key.R))
                Task.Run(() => selected.Refresh(CancellationToken.None));

            ImGui.SameLine();

            if (ImGui.Button("Fetch (Ctrl+F)") || input.Ctrl(Key.F))
                Task.Run(() => selected.Fetch(CancellationToken.None));
        }
    }
}

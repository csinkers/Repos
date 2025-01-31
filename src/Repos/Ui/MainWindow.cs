using ImGuiNET;
using Repos.Core;
using SharpFileDialog;
using Veldrid;

namespace Repos.Ui;

public class MainWindow(RepoManager repoManager)
{
    public void Draw(Input input)
    {
        ImGui.BeginDisabled(repoManager.BusyMessage != null);

        if (ImGui.Button("Add Repo (Ctrl+A)") || input.Ctrl(Key.A))
        {
            var dialog = new DirectoryDialog("Select a repository");
            dialog.Open(x =>
            {
                if (
                    x.Success
                    && Directory.Exists(x.FileName)
                    && Directory.Exists(Path.Combine(x.FileName, ".git"))
                )
                {
                    repoManager.AddRepo(x.FileName);
                }
            });
        }

        ImGui.SameLine();
        if (ImGui.Button("Add All Repos in Directory (Ctrl+Shift+A)") || input.ShiftCtrl(Key.A))
        {
            var dialog = new DirectoryDialog("Select directory containing repos");
            dialog.Open(x =>
            {
                if (x.Success && Directory.Exists(x.FileName))
                    repoManager.AddReposInDirectory(x.FileName);
            });
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

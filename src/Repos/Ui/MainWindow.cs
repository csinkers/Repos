using Repos.Core;

namespace Repos.Ui;

public class MainWindow(RepoManager repoManager)
{
    public void Draw()
    {
        TreeNode.DrawTree(repoManager.Tree);
    }
}
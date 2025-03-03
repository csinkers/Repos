using LibGit2Sharp;

namespace Repos.Core;

public class RepoFactory
{
    public IRepo Create(string path)
    {
        try
        {
            var repository = new Repository(path);
            return new Repo(path, repository);
        }
        catch (RepositoryNotFoundException)
        {
            return new MissingRepo(path, "repo not found");
        }
        catch (LibGit2SharpException ex)
        {
            return new MissingRepo(path, ex.Message);
        }
    }
}

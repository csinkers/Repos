namespace Repos.Model;

public record BranchStatus(string BranchName, bool IsTracked, int Ahead, int Behind);

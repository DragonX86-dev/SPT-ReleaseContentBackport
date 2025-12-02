namespace ReleaseContentBackport;

public static class ModExtensions
{
    public static string CombinePaths(this List<string> folders)
    {
        return folders.Aggregate(string.Empty, Path.Combine);
    } 
}
namespace DataBit.StockIA.Platform.Tests;

internal static class TestPaths
{
    /// <summary>ml/models of the repository (walks up from the test binaries).</summary>
    public static string ModelsFolder
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "ml", "models")))
                directory = directory.Parent;
            return directory == null
                ? throw new DirectoryNotFoundException("ml/models not found")
                : Path.Combine(directory.FullName, "ml", "models");
        }
    }
}

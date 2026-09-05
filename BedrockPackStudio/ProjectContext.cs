namespace BedrockPackStudio;

public static class ProjectContext
{
    public static string? CurrentProjectPath { get; set; }

    public static string? CurrentFilePath { get; set; }

    public static string? CurrentTexturePath { get; set; }

    public static bool HasProject =>
        !string.IsNullOrWhiteSpace(CurrentProjectPath) &&
        Directory.Exists(CurrentProjectPath);

    public static void Clear()
    {
        CurrentProjectPath = null;
        CurrentFilePath = null;
        CurrentTexturePath = null;
    }
}

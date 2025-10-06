namespace Tranzor.Utils;

public class FileUtil
{
    public static string? ResolvePath(string path)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string basePath = Path.Combine(baseDirectory, path);

        if (File.Exists(basePath))
            return basePath;

        string rootPath = Path.Combine(baseDirectory, "..", "..", "..", "..", path);
        if (File.Exists(rootPath))
            return rootPath;

        return null;
    }
}

using System.IO;

namespace AuraEcho.Core.Tools;

public static class FileUtil
{
    public static bool IsFileLocked(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        try
        {
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }
}

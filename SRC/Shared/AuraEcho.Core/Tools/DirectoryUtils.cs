using System.IO;

namespace AuraEcho.Core.Tools;

/// <summary>
/// 目录操作工具类
/// </summary>
public static class DirectoryUtils
{
    public static void SafeMoveDirectory(string sourceDir, string destinationDir)
    {
        if (Path.GetPathRoot(sourceDir) == Path.GetPathRoot(destinationDir))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationDir));
            // 同一卷，直接移动
            Directory.Move(sourceDir, destinationDir);
            return;
        }

        // 跨卷，递归复制再删除
        CopyDirectory(sourceDir, destinationDir);
        Directory.Delete(sourceDir, recursive: true);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        // 复制所有文件
        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destFile, overwrite: true);
        }

        // 递归复制所有子目录
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    public static bool AreDirectoriesEqual(DirectoryInfo dir1, DirectoryInfo dir2)
    {
        if (dir1 == null || dir2 == null)
            return dir1 == dir2;

        return AreDirectoriesEqual(dir1.FullName, dir2.FullName);
    }

    public static bool AreDirectoriesEqual(string dir1, string dir2)
    {
        if (String.IsNullOrWhiteSpace(dir1) || String.IsNullOrWhiteSpace(dir1))
            return dir1 == dir2;

        if (!Directory.Exists(dir1) || !Directory.Exists(dir2))
            return false;

        // 去除结尾的路径分隔符
        string path1 = dir1.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string path2 = dir2.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // 不区分大小写比较
        return String.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);
    }

}

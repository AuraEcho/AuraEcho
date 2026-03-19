namespace AuraEcho.Core.Tools;

using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class SecureStore
{
    public static void Save(string key, string value)
    {
        var data = Encoding.UTF8.GetBytes(value);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetPath(key), encrypted);
    }

    public static string Load(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path)) return null;

        var encrypted = File.ReadAllBytes(path);
        var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(data);
    }

    public static void Delete(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    public static string GetPath(string key) => Path.Combine(ApplicationPaths.SecureStore, key + ".bin");
}

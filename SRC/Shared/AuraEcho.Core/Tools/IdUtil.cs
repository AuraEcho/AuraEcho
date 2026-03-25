namespace AuraEcho.Core.Tools;

public static class IdUtil
{
    public static string GetShortId(int length)
    {
        if (length <= 0) throw new ArgumentException();

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}

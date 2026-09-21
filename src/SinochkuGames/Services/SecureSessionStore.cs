using System.Security.Cryptography;
using System.Text;

namespace SinochkuGames.Services;

public sealed class SecureSessionStore
{
    private static string SessionFile => Path.Combine(AppPaths.Root, "social-session.bin");

    public string? LoadToken()
    {
        try
        {
            if (!File.Exists(SessionFile)) return null;
            var encrypted = File.ReadAllBytes(SessionFile);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            LogService.Warn($"Could not restore social session: {ex.Message}");
            return null;
        }
    }

    public void SaveToken(string token)
    {
        Directory.CreateDirectory(AppPaths.Root);
        var plain = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(SessionFile, encrypted);
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(SessionFile))
                File.Delete(SessionFile);
        }
        catch { }
    }
}

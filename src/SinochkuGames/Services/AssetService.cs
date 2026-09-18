using System.Reflection;

namespace SinochkuGames.Services;

public static class AssetService
{
    public static Image? LoadBySuffix(string suffix)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            if (resource is null) return null;

            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null) return null;

            using var temp = Image.FromStream(stream);
            return new Bitmap(temp);
        }
        catch (Exception ex)
        {
            LogService.Warn($"Could not load embedded asset {suffix}: {ex.Message}");
            return null;
        }
    }
}

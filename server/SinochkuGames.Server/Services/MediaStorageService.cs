namespace SinochkuGames.Server.Services;

public sealed class MediaStorageService(IWebHostEnvironment env)
{
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    public async Task<string> SaveAsync(IFormFile file, string category, long maxBytes, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > maxBytes)
            throw new InvalidOperationException($"Image must be between 1 byte and {maxBytes / 1024 / 1024} MB.");

        if (!Allowed.TryGetValue(file.ContentType, out var extension))
            throw new InvalidOperationException("Only PNG, JPEG, and WebP images are allowed.");

        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "uploads", category);
        Directory.CreateDirectory(folder);

        var name = $"{Guid.NewGuid():N}{extension}";
        var full = Path.Combine(folder, name);

        await using var stream = File.Create(full);
        await file.CopyToAsync(stream, ct);

        return $"/uploads/{category}/{name}";
    }
}

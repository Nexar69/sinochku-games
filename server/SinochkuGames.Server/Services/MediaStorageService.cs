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

        await using (var probe = file.OpenReadStream())
        {
            var header = new byte[12];
            var read = await probe.ReadAsync(header.AsMemory(0, header.Length), ct);
            if (!LooksLikeImage(file.ContentType, header.AsSpan(0, read)))
                throw new InvalidOperationException("The uploaded file does not match its image type.");
        }

        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "uploads", category);
        Directory.CreateDirectory(folder);

        var name = $"{Guid.NewGuid():N}{extension}";
        var full = Path.Combine(folder, name);

        await using var stream = File.Create(full);
        await file.CopyToAsync(stream, ct);

        return $"/uploads/{category}/{name}";
    }

    private static bool LooksLikeImage(string contentType, ReadOnlySpan<byte> bytes)
    {
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 8
                   && bytes[0] == 0x89
                   && bytes[1] == 0x50
                   && bytes[2] == 0x4E
                   && bytes[3] == 0x47
                   && bytes[4] == 0x0D
                   && bytes[5] == 0x0A
                   && bytes[6] == 0x1A
                   && bytes[7] == 0x0A;

        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 3
                   && bytes[0] == 0xFF
                   && bytes[1] == 0xD8
                   && bytes[2] == 0xFF;

        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 12
                   && bytes[0] == (byte)'R'
                   && bytes[1] == (byte)'I'
                   && bytes[2] == (byte)'F'
                   && bytes[3] == (byte)'F'
                   && bytes[8] == (byte)'W'
                   && bytes[9] == (byte)'E'
                   && bytes[10] == (byte)'B'
                   && bytes[11] == (byte)'P';

        return false;
    }
}

using System.Text;

namespace SinochkuGames.Services;

public static class LogService
{
    private static readonly object Gate = new();
    private static string _file = "";

    public static void Initialize()
    {
        AppPaths.EnsureCreated();
        _file = Path.Combine(AppPaths.LogDirectory, $"launcher-{DateTime.Now:yyyy-MM-dd}.log");
        Info("Logging initialized");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}{Environment.NewLine}{ex}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                File.AppendAllText(
                    _file,
                    $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch { }
    }
}

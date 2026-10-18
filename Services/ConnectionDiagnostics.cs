namespace OBLSequenceStudio.Services;

/// <summary>Writes synchronized connection diagnostics without allowing log failures to interrupt device workflows.</summary>
internal static class ConnectionDiagnostics
{
    private static readonly object Gate = new();
    /// <summary>Per-user diagnostic file path retained under the historical ModbusSequenceStudio directory.</summary>
    public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModbusSequenceStudio", "connection.log");
    /// <summary>Starts a new discovery log, replacing previous contents when the file can be written.</summary>
    public static void Begin()
    {
        lock (Gate)
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!); File.WriteAllText(LogPath, $"Connection search {DateTimeOffset.Now:O}{Environment.NewLine}"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }
    /// <summary>Writes a timestamped message to Debug and the file, rotating files larger than 4 MiB to .previous.</summary>
    public static void Write(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 4 * 1024 * 1024) File.Move(LogPath, LogPath + ".previous", true);
                File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }
}

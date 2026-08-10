using System;
using System.IO;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Services;

public static class CrashLogger
{
    private static readonly object SyncRoot = new();

    public static void Log(Exception exception)
    {
        try
        {
            lock (SyncRoot)
            {
                var directory = PathHelper.GetDataDirectory();
                Directory.CreateDirectory(directory);

                var file = Path.Combine(directory, "crash.log");
                File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {exception}\n\n");
            }
        }
        catch
        {
            // Never let logging itself crash the app.
        }
    }
}

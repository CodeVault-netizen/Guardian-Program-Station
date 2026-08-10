using System.Diagnostics;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Helpers;

namespace Guardian.ProgramStation.Infrastructure.Indexing;

public sealed class FileSystemIndexer : IIndexingService
{
    public Task<IReadOnlyList<ProgramModel>> IndexAsync(IEnumerable<string> folderPaths, CancellationToken cancellationToken = default)
    {
        var programs = new List<ProgramModel>();

        foreach (var folder in folderPaths)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(
                folder,
                "*",
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                }))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ExecutableDetector.IsExecutable(file))
                {
                    continue;
                }

                programs.Add(new ProgramModel
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file,
                    Version = GetFileVersion(file),
                    ExecutableType = ExecutableDetector.DetectType(file),
                    Size = GetSize(file),
                    AddedAt = GetLastWriteTimeUtc(file),
                });
            }
        }

        return Task.FromResult<IReadOnlyList<ProgramModel>>(programs);
    }

    private static string GetFileVersion(string filePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(filePath);
            return string.IsNullOrWhiteSpace(info.FileVersion) ? string.Empty : info.FileVersion;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static long GetSize(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);
            return info.Exists ? info.Length : 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static DateTime GetLastWriteTimeUtc(string filePath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(filePath);
        }
        catch (Exception)
        {
            return DateTime.UtcNow;
        }
    }
}

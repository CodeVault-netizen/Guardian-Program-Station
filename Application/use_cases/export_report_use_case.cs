using System.Text;
using System.Text.Json;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.UseCases;

public sealed class ExportReportUseCase
{
    private readonly IStorageService _storageService;

    public ExportReportUseCase(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task ExportToCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Name,Version,Path,Link,License,Notes");

        foreach (var program in programs)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(program.Name),
                EscapeCsv(program.Version),
                EscapeCsv(program.Path),
                EscapeCsv(program.Link),
                EscapeCsv(program.License),
                EscapeCsv(program.Notes)));
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportToJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var programs = await _storageService.LoadProgramsAsync(cancellationToken);

        var json = JsonSerializer.Serialize(programs, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

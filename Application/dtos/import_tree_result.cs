using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Application.Dtos;

public sealed class ImportTreeResult
{
    public required TreeModel Tree { get; init; }

    public int AddedProgramsCount { get; init; }
}

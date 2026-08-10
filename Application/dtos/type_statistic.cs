using Guardian.ProgramStation.Core.Enums;

namespace Guardian.ProgramStation.Application.Dtos;

public sealed class TypeStatistic
{
    public ExecutableType Type { get; set; }

    public int Count { get; set; }

    public double Percentage { get; set; }
}

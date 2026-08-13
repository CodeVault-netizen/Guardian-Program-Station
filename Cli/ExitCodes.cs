namespace Guardian.ProgramStation.Cli;

/// <summary>
/// Exit code contract for the CLI:
/// 0 = success, 1 = general error, 2 = invalid arguments,
/// 3 = validation error, 4 = operation failed.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;

    public const int GeneralError = 1;

    public const int InvalidArguments = 2;

    public const int ValidationError = 3;

    public const int OperationFailed = 4;
}

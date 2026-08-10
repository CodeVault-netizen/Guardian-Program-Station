using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Kernel;

public static class DependencyInjection
{
    public static ServiceProvider BuildServiceProvider()
        => new ServiceCollection().AddGuardianProgramStation().BuildServiceProvider();
}

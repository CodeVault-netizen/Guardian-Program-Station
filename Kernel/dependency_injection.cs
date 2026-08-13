using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Kernel;

public static class DependencyInjection
{
    /// <summary>
    /// Builds the application service provider. The optional <paramref name="configure"/>
    /// hook lets the UI composition root register UI-specific services (like the
    /// Avalonia-backed system clipboard service) that the Kernel cannot see.
    /// </summary>
    public static ServiceProvider BuildServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddGuardianProgramStation();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }
}

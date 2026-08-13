using System.Text;
using Guardian.ProgramStation.Cli;
using Guardian.ProgramStation.Kernel;

// Ensure the box-drawing characters used by the preview (and any non-ASCII
// output) render correctly on every platform, including when redirected.
Console.OutputEncoding = Encoding.UTF8;

// The CLI uses the exact same composition root as the GUI (Kernel), so both
// entry points share the Application use cases and Infrastructure services.
using var services = DependencyInjection.BuildServiceProvider();

return await CliApplication.RunAsync(args, services);

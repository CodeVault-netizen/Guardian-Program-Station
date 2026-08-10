using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;

namespace Guardian.ProgramStation.Infrastructure.Scheduling;

public sealed class IndexingScheduler : IAsyncDisposable
{
    private readonly IndexProgramsUseCase _indexProgramsUseCase;
    private readonly ISettingsService _settingsService;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public IndexingScheduler(IndexProgramsUseCase indexProgramsUseCase, ISettingsService settingsService)
    {
        _indexProgramsUseCase = indexProgramsUseCase;
        _settingsService = settingsService;
    }

    public void Start()
    {
        Stop();

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync()
    {
        Stop();

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _loop = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var period = ResolveInterval(settings.ScheduleInterval);

            if (settings.AutoIndexEnabled)
            {
                try
                {
                    await _indexProgramsUseCase.ExecuteAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                }
            }

            using var timer = new PeriodicTimer(period);
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static TimeSpan ResolveInterval(string scheduleInterval)
        => scheduleInterval switch
        {
            "hourly" => TimeSpan.FromHours(1),
            "weekly" => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1),
        };
}

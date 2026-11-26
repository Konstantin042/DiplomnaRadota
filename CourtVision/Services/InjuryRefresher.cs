// Фонова задача: на всеки 10 минути взима доклада за контузиите от ESPN и обновява InjuredPlayers
using CourtVision.Database;

namespace CourtVision.Services;

public class InjuryRefresher : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(10);
    private readonly IHttpClientFactory _http;
    private readonly IServiceScopeFactory _scopes;
    private readonly InjuryService _injuries;
    private readonly ILogger<InjuryRefresher> _log;

    public InjuryRefresher(IHttpClientFactory http, IServiceScopeFactory scopes, InjuryService injuries, ILogger<InjuryRefresher> log)
    {
        _http = http; _scopes = scopes; _injuries = injuries; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                var json = await _http.CreateClient("espn").GetStringAsync(InjuryService.FeedUrl, stop);
                var list = InjuryService.Parse(json);
                using var scope = _scopes.CreateScope();
                _injuries.Apply(list, scope.ServiceProvider.GetRequiredService<AppDbContext>());
                _log.LogInformation("Injury report updated: {Count} players", list.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Без интернет сайтът продължава с последния запазен доклад
                _injuries.Failed(ex.Message);
                _log.LogWarning("Injury report not updated: {Error}", ex.Message);
            }
            await Task.Delay(Every, stop);
        }
    }
}

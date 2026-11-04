// Записва изиграните игри в таблицата GameResults и строи класациите и профила
using System.Security.Claims;
using CourtVision.Database;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Services;

// Описание на една игра за класациите: ключ, име, какво означава резултатът и дали по-малкото е по-добро
public record GameInfo(string Key, string Name, string ScoreLabel, bool LowerIsBetter);

// Един ред от класацията: име, най-добър резултат и брой изиграни игри
public record LeaderRow(string Name, int Best, int Played);

// Класацията на една игра за изгледа: описанието на играта и редовете
public record LeaderboardBox(GameInfo Game, List<LeaderRow> Rows);

// Услугата за резултатите (Scoped: нова за всяка заявка, заедно с връзката с базата)
public class ResultService
{
    public static readonly IReadOnlyList<GameInfo> Games = new List<GameInfo>
    {
        new("challenge", "82-0", "wins", false),
        new("build", "Build-a-Player", "season wins", false),
        new("hilo", "Higher or Lower", "streak", false),
        new("salary", "Salary Cap Game", "OVR", false),
        new("quiz", "Position Quiz", "of 8 correct", false),
        new("guess", "Guess the Player", "guesses", true),
    };

    private readonly AppDbContext _db;

    public ResultService(AppDbContext db) => _db = db;

    // Идентификаторът на влезлия потребител (null за гост)
    public static string? UserId(HttpContext http) =>
        http.User.Identity?.IsAuthenticated == true ? http.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    // Името за класациите: потребителското име или "Guest 1234" (от сесията)
    public static string PlayerName(HttpContext http) =>
        http.User.Identity?.IsAuthenticated == true && http.User.Identity.Name is { Length: > 0 } name
            ? name
            : SalaryLeaderboard.GuestName(http.Session.Id);

    // Описанието на игра по ключ
    public static GameInfo Info(string game) => Games.First(g => g.Key == game);

    // Записва една изиграна игра
    public void Save(HttpContext http, string game, int score, string details)
    {
        _db.GameResults.Add(new GameResult
        {
            UserId = UserId(http),
            PlayerName = PlayerName(http),
            Game = game,
            Score = score,
            Details = details.Length > 200 ? details[..200] : details,
            PlayedAt = DateTime.UtcNow,
        });
        // Записваме в базата (INSERT)
        _db.SaveChanges();
    }

    // Класацията на една игра: най-добрият резултат на всеки играч, първите count места
    public List<LeaderRow> Top(string game, int count = 10)
    {
        var info = Info(game);
        var results = _db.GameResults.AsNoTracking().Where(r => r.Game == game);
        if (info.LowerIsBetter) results = results.Where(r => r.Score > 0);
        // Групираме по играч (GROUP BY в SQL)
        var groups = results.GroupBy(r => r.PlayerName);
        var rows = info.LowerIsBetter
            ? groups.Select(g => new { Name = g.Key, Best = g.Min(r => r.Score), Played = g.Count() })
                .OrderBy(x => x.Best).ThenByDescending(x => x.Played).Take(count).ToList()
            : groups.Select(g => new { Name = g.Key, Best = g.Max(r => r.Score), Played = g.Count() })
                .OrderByDescending(x => x.Best).ThenByDescending(x => x.Played).Take(count).ToList();
        return rows.Select(x => new LeaderRow(x.Name, x.Best, x.Played)).ToList();
    }

    // Последните игри на един потребител (за страницата Profile)
    public List<GameResult> Recent(string userId, int count = 20) =>
        _db.GameResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.PlayedAt)
            .Take(count)
            .ToList();

    // Най-добрият резултат и броят игри на потребител във всяка игра
    public Dictionary<string, LeaderRow> Bests(string userId)
    {
        var result = new Dictionary<string, LeaderRow>();
        foreach (var info in Games)
        {
            var mine = _db.GameResults.AsNoTracking().Where(r => r.UserId == userId && r.Game == info.Key);
            var played = mine.Count();
            if (played == 0) continue;
            var counted = mine.Where(r => !info.LowerIsBetter || r.Score > 0);
            var best = !counted.Any() ? 0 : info.LowerIsBetter ? counted.Min(r => r.Score) : counted.Max(r => r.Score);
            result[info.Key] = new LeaderRow(info.Name, best, played);
        }
        return result;
    }

    // Мястото на потребителя в класацията на игра (null, ако го няма в първите 100)
    public int? Rank(string game, string playerName)
    {
        var top = Top(game, 100);
        var index = top.FindIndex(r => r.Name == playerName);
        return index < 0 ? null : index + 1;
    }
}

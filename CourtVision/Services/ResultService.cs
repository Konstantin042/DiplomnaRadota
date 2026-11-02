// Записва изиграните игри в таблицата GameResults и строи класациите и профила
using System.Security.Claims;   // ClaimTypes – идентификаторът на влезлия потребител
using CourtVision.Database;      // Базата данни (AppDbContext и таблиците)
using Microsoft.EntityFrameworkCore; // AsNoTracking за заявките само за четене

namespace CourtVision.Services; // Пространство от имена за услугите

// Описание на една игра за класациите: ключ, име, какво означава резултатът и дали по-малкото е по-добро
public record GameInfo(string Key, string Name, string ScoreLabel, bool LowerIsBetter);

// Един ред от класацията: име, най-добър резултат и брой изиграни игри
public record LeaderRow(string Name, int Best, int Played);

// Класацията на една игра за изгледа: описанието на играта и редовете
public record LeaderboardBox(GameInfo Game, List<LeaderRow> Rows);

// Услугата за резултатите (Scoped: нова за всяка заявка, заедно с връзката с базата)
public class ResultService
{
    // Всички игри, които имат класация (в реда, в който се показват)
    public static readonly IReadOnlyList<GameInfo> Games = new List<GameInfo>
    {
        new("challenge", "82-0", "wins", false),                  // Победи от 82 мача
        new("build", "Build-a-Player", "season wins", false),     // Победи в сезона
        new("hilo", "Higher or Lower", "streak", false),          // Най-дълга серия
        new("salary", "Salary Cap Game", "OVR", false),           // Обща оценка на билда
        new("quiz", "Position Quiz", "of 8 correct", false),      // Верни отговори от 8
        new("guess", "Guess the Player", "guesses", true),        // Най-малко опити (само победи)
    };

    // Връзката с базата данни
    private readonly AppDbContext _db;

    // Конструктор: получава връзката с базата
    public ResultService(AppDbContext db) => _db = db;

    // Идентификаторът на влезлия потребител (null за гост)
    public static string? UserId(HttpContext http) =>
        http.User.Identity?.IsAuthenticated == true ? http.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    // Името за класациите: потребителското име или "Guest 1234" (от сесията)
    public static string PlayerName(HttpContext http) =>
        http.User.Identity?.IsAuthenticated == true && http.User.Identity.Name is { Length: > 0 } name
            ? name                                         // Влязъл потребител
            : SalaryLeaderboard.GuestName(http.Session.Id); // Гост

    // Описанието на игра по ключ
    public static GameInfo Info(string game) => Games.First(g => g.Key == game);

    // Записва една изиграна игра
    public void Save(HttpContext http, string game, int score, string details)
    {
        // Новият ред в таблицата GameResults
        _db.GameResults.Add(new GameResult
        {
            UserId = UserId(http),                                 // Кой е играл (или NULL)
            PlayerName = PlayerName(http),                         // Име за класацията
            Game = game,                                           // Коя игра
            Score = score,                                         // Резултатът
            Details = details.Length > 200 ? details[..200] : details, // Описание (най-много 200 знака)
            PlayedAt = DateTime.UtcNow,                            // Кога
        });
        // Записваме в базата (INSERT)
        _db.SaveChanges();
    }

    // Класацията на една игра: най-добрият резултат на всеки играч, първите count места
    public List<LeaderRow> Top(string game, int count = 10)
    {
        // Описанието на играта (дали по-малкото е по-добро)
        var info = Info(game);
        // Само резултатите от тази игра
        var results = _db.GameResults.AsNoTracking().Where(r => r.Game == game);
        // При Guess the Player се броят само победите (резултат над 0)
        if (info.LowerIsBetter) results = results.Where(r => r.Score > 0);
        // Групираме по играч (GROUP BY в SQL)
        var groups = results.GroupBy(r => r.PlayerName);
        // Най-добрият резултат на всеки играч и броят му игри, подредени и ограничени до count реда
        var rows = info.LowerIsBetter
            ? groups.Select(g => new { Name = g.Key, Best = g.Min(r => r.Score), Played = g.Count() }) // Най-малко опити
                .OrderBy(x => x.Best).ThenByDescending(x => x.Played).Take(count).ToList()
            : groups.Select(g => new { Name = g.Key, Best = g.Max(r => r.Score), Played = g.Count() }) // Най-голям резултат
                .OrderByDescending(x => x.Best).ThenByDescending(x => x.Played).Take(count).ToList();
        // Превръщаме ги в редове за изгледа
        return rows.Select(x => new LeaderRow(x.Name, x.Best, x.Played)).ToList();
    }

    // Последните игри на един потребител (за страницата Profile)
    public List<GameResult> Recent(string userId, int count = 20) =>
        _db.GameResults.AsNoTracking()                 // Само четем
            .Where(r => r.UserId == userId)            // Само неговите
            .OrderByDescending(r => r.PlayedAt)        // Най-новите първи
            .Take(count)                               // Първите count
            .ToList();                                 // Изпълняваме заявката

    // Най-добрият резултат и броят игри на потребител във всяка игра
    public Dictionary<string, LeaderRow> Bests(string userId)
    {
        // Резултатът: ключ на играта → ред
        var result = new Dictionary<string, LeaderRow>();
        // Обхождаме всички игри
        foreach (var info in Games)
        {
            // Неговите резултати в тази игра (при Guess the Player само победите за най-добър резултат)
            var mine = _db.GameResults.AsNoTracking().Where(r => r.UserId == userId && r.Game == info.Key);
            // Колко пъти е играл
            var played = mine.Count();
            // Ако не е играл, пропускаме
            if (played == 0) continue;
            // Резултатите, които се броят за най-добър
            var counted = mine.Where(r => !info.LowerIsBetter || r.Score > 0);
            // Най-добрият резултат (0, ако няма нито една победа в Guess the Player)
            var best = !counted.Any() ? 0 : info.LowerIsBetter ? counted.Min(r => r.Score) : counted.Max(r => r.Score);
            // Записваме реда
            result[info.Key] = new LeaderRow(info.Name, best, played);
        }
        // Връщаме всички
        return result;
    }

    // Мястото на потребителя в класацията на игра (null, ако го няма в първите 100)
    public int? Rank(string game, string playerName)
    {
        // Първите 100 места
        var top = Top(game, 100);
        // Търсим името
        var index = top.FindIndex(r => r.Name == playerName);
        // Мястото започва от 1
        return index < 0 ? null : index + 1;
    }
}

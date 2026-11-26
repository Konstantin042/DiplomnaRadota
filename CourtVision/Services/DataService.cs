// Зарежда всички данни при стартиране: играчите и отборите от базата данни, индексите на снимките от JSON
using System.Globalization;
using System.Text;
using System.Text.Json;
using CourtVision.Database;
using CourtVision.Models;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Services;

// Услуга, която държи всички данни в паметта (чете ги от базата веднъж, после игрите са бързи)
public class DataService
{
    public IReadOnlyDictionary<string, List<PoolPlayer>> Pools { get; }
    public IReadOnlyList<string> PoolKeys { get; }
    public IReadOnlyList<BuildPlayer> BuildPlayers { get; }
    public IReadOnlyList<QuizPlayer> QuizPlayers { get; }
    public IReadOnlyDictionary<string, int[]> NbaPhotos { get; }
    public IReadOnlyDictionary<string, int[]> BbrefPhotos { get; }
    public IReadOnlyDictionary<string, string> NameToId { get; }
    // Сегашният отбор (сезон 2026–27) на всеки играч от съставите: ключ CurrentKey(име) → съкращение на отбора
    public IReadOnlyDictionary<string, string> CurrentTeams { get; }

    // Конструктор: изпълнява се веднъж, при първото използване на услугата
    public DataService(IServiceScopeFactory scopes, IWebHostEnvironment env)
    {
        // Създаваме временен обхват, за да вземем връзка с базата (тя живее само в него)
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Pools = LoadPools(db);
        PoolKeys = Pools.Keys.ToList();
        BuildPlayers = LoadBuildPlayers(db);
        QuizPlayers = LoadQuizPlayers(db);
        var current = new Dictionary<string, string>();
        foreach (var c in db.CurrentPlayers.AsNoTracking().ToList())
            current.TryAdd(CurrentKey(c.Name), c.TeamAbbr);
        CurrentTeams = current;
        var dir = Path.Combine(env.ContentRootPath, "Data");
        NbaPhotos = LoadPhotos(Path.Combine(dir, "photos-nba.json"));
        BbrefPhotos = LoadPhotos(Path.Combine(dir, "photos-bbref.json"));
        var names = new Dictionary<string, string>();
        foreach (var player in Pools.Values.SelectMany(list => list))
            names.TryAdd(NormalizeName(player.Name), player.Id);
        NameToId = names;
    }

    // Чете таблица RosterPlayers и я превръща в речник "ОТБОР|десетилетие" → играчи
    private static Dictionary<string, List<PoolPlayer>> LoadPools(AppDbContext db)
    {
        var rows = db.RosterPlayers.AsNoTracking().OrderBy(r => r.SortOrder).ToList();
        var result = new Dictionary<string, List<PoolPlayer>>();
        foreach (var r in rows)
        {
            var key = r.TeamAbbr + "|" + r.Decade;
            if (!result.TryGetValue(key, out var list)) result[key] = list = new List<PoolPlayer>();
            list.Add(new PoolPlayer
            {
                Name = r.Name,
                Id = r.PlayerCode,
                Positions = r.Positions.Split('/').ToList(),
                Team = r.TeamAbbr,
                Decade = r.Decade,
                Ppg = r.Ppg,
                Rpg = r.Rpg,
                Apg = r.Apg,
                Spg = r.Spg,
                Bpg = r.Bpg,
            });
        }
        return result;
    }

    // Чете таблиците RatedPlayers и PlayerGrades и ги превръща в играчи за Build-a-Player
    private static List<BuildPlayer> LoadBuildPlayers(AppDbContext db)
    {
        // Играчите заедно с оценките им (Include прави JOIN с таблицата PlayerGrades)
        var rows = db.RatedPlayers.AsNoTracking().Include(p => p.Grades).OrderBy(p => p.SortOrder).ToList();
        return rows.Select((p, index) => new BuildPlayer
        {
            Index = index,
            Mode = p.Mode,
            Group = p.Group,
            Name = p.Name,
            Team = p.TeamAbbr,
            Years = p.Years,
            Height = p.Height,
            Weight = p.Weight,
            Id = p.PlayerCode,
            Rookie = p.Rookie,
            Grades = p.Grades.OrderBy(g => g.Slot).Select(g => g.Value).ToArray(),
            RealStats = p.Ppg == null
                ? Array.Empty<double>()
                : new[] { p.Ppg.Value, p.Rpg ?? 0, p.Apg ?? 0, p.Spg ?? 0, p.Bpg ?? 0 },
        }).ToList();
    }

    // Чете таблица QuizEntries и я превръща в играчи за куиза
    private static List<QuizPlayer> LoadQuizPlayers(AppDbContext db) =>
        db.QuizEntries.AsNoTracking().OrderBy(q => q.Id).ToList()
            .Select(q => new QuizPlayer
            {
                Name = q.Name,
                Positions = q.Positions.Split('/').ToList(),
                Team = q.TeamAbbr,
                Decade = q.Decade,
                Overall = q.Overall,
                Skills = new[] { q.JumpShot, q.Handles, q.Finishing, q.Defense, q.Size },
                Stats = new[] { q.Ppg, q.Rpg, q.Apg, q.Spg, q.Bpg },
            }).ToList();

    // Чете индекс на снимки: {"ключ": [номер на изображение, позиция в него]}
    private static Dictionary<string, int[]> LoadPhotos(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(path)) ?? new()
            : new();

    // Превръща име в прост ключ: без ударения, малки букви, само латиница
    public static string NormalizeName(string name)
    {
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            var lower = char.ToLowerInvariant(ch);
            if (lower >= 'a' && lower <= 'z') sb.Append(lower);
        }
        return sb.ToString();
    }

    // Ключ за сравнение на имена от различни източници: без Jr./Sr./II/III/IV ("Jimmy Butler III" = "Jimmy Butler")
    public static string CurrentKey(string name)
    {
        var suffixes = new[] { "jr", "sr", "ii", "iii", "iv" };
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !suffixes.Contains(w.Trim('.', ',').ToLowerInvariant()));
        return NormalizeName(string.Join(" ", words));
    }
}

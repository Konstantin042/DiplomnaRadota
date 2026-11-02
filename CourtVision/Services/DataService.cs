// Зарежда всички данни при стартиране: играчите и отборите от базата данни, индексите на снимките от JSON
using System.Globalization;      // За нормализиране на имена (махане на ударения)
using System.Text;                // За работа с низове (StringBuilder, нормализация)
using System.Text.Json;           // За четене на JSON файловете със снимките
using CourtVision.Database;       // Базата данни (AppDbContext и таблиците)
using CourtVision.Models;         // Нашите модели на данни
using Microsoft.EntityFrameworkCore; // AsNoTracking и Include за заявките към базата

namespace CourtVision.Services; // Пространство от имена за услугите

// Услуга, която държи всички данни в паметта (чете ги от базата веднъж, после игрите са бързи)
public class DataService
{
    // Съставите за 82-0: ключ "ОТБОР|десетилетие" → списък с играчи
    public IReadOnlyDictionary<string, List<PoolPlayer>> Pools { get; }
    // Всички ключове на съставите (за случайно завъртане)
    public IReadOnlyList<string> PoolKeys { get; }
    // Играчите за Build-a-Player
    public IReadOnlyList<BuildPlayer> BuildPlayers { get; }
    // Играчите за Position Quiz
    public IReadOnlyList<QuizPlayer> QuizPlayers { get; }
    // Индекс на официалните NBA снимки: ключ → [номер на изображение, позиция]
    public IReadOnlyDictionary<string, int[]> NbaPhotos { get; }
    // Индекс на снимките от Basketball-Reference: ключ → [номер, позиция]
    public IReadOnlyDictionary<string, int[]> BbrefPhotos { get; }
    // Нормализирано име → идентификатор (за играчи, за които знаем само името)
    public IReadOnlyDictionary<string, string> NameToId { get; }

    // Конструктор: изпълнява се веднъж, при първото използване на услугата
    public DataService(IServiceScopeFactory scopes, IWebHostEnvironment env)
    {
        // Създаваме временен обхват, за да вземем връзка с базата (тя живее само в него)
        using var scope = scopes.CreateScope();
        // Връзката с базата данни
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Съставите: таблица RosterPlayers, групирана по отбор и десетилетие
        Pools = LoadPools(db);
        // Запомняме ключовете в списък
        PoolKeys = Pools.Keys.ToList();
        // Играчите за Build-a-Player: таблици RatedPlayers и PlayerGrades
        BuildPlayers = LoadBuildPlayers(db);
        // Играчите за куиза: таблица QuizEntries
        QuizPlayers = LoadQuizPlayers(db);
        // Пътят до папката с индексите на снимките
        var dir = Path.Combine(env.ContentRootPath, "Data");
        // Зареждаме индекса на NBA снимките
        NbaPhotos = LoadPhotos(Path.Combine(dir, "photos-nba.json"));
        // Зареждаме индекса на резервните снимки
        BbrefPhotos = LoadPhotos(Path.Combine(dir, "photos-bbref.json"));
        // Строим речник име → идентификатор от съставите
        var names = new Dictionary<string, string>();
        // Обхождаме всички играчи във всички състави
        foreach (var player in Pools.Values.SelectMany(list => list))
            // Пазим първия срещнат идентификатор за всяко име
            names.TryAdd(NormalizeName(player.Name), player.Id);
        // Записваме готовия речник
        NameToId = names;
    }

    // Чете таблица RosterPlayers и я превръща в речник "ОТБОР|десетилетие" → играчи
    private static Dictionary<string, List<PoolPlayer>> LoadPools(AppDbContext db)
    {
        // Всички редове, подредени както са записани (AsNoTracking: само четем, не променяме)
        var rows = db.RosterPlayers.AsNoTracking().OrderBy(r => r.SortOrder).ToList();
        // Резултатът: празен речник
        var result = new Dictionary<string, List<PoolPlayer>>();
        // Обхождаме всеки ред
        foreach (var r in rows)
        {
            // Ключът на състава
            var key = r.TeamAbbr + "|" + r.Decade;
            // Ако съставът още го няма, създаваме празен списък
            if (!result.TryGetValue(key, out var list)) result[key] = list = new List<PoolPlayer>();
            // Превръщаме реда от базата в модела, който използват игрите
            list.Add(new PoolPlayer
            {
                Name = r.Name,                                   // Име
                Id = r.PlayerCode,                               // Идентификатор
                Positions = r.Positions.Split('/').ToList(),     // Позиции
                Team = r.TeamAbbr,                               // Отбор
                Decade = r.Decade,                               // Десетилетие
                Ppg = r.Ppg,                                     // Точки
                Rpg = r.Rpg,                                     // Борби
                Apg = r.Apg,                                     // Асистенции
                Spg = r.Spg,                                     // Кражби или null
                Bpg = r.Bpg,                                     // Блокади или null
            });
        }
        // Връщаме всички състави
        return result;
    }

    // Чете таблиците RatedPlayers и PlayerGrades и ги превръща в играчи за Build-a-Player
    private static List<BuildPlayer> LoadBuildPlayers(AppDbContext db)
    {
        // Играчите заедно с оценките им (Include прави JOIN с таблицата PlayerGrades)
        var rows = db.RatedPlayers.AsNoTracking().Include(p => p.Grades).OrderBy(p => p.SortOrder).ToList();
        // Превръщаме всеки ред в модела; номерът в списъка е и ключът на играча
        return rows.Select((p, index) => new BuildPlayer
        {
            Index = index,                                                       // Пореден номер
            Mode = p.Mode,                                                       // Режим
            Group = p.Group,                                                     // Група
            Name = p.Name,                                                       // Име
            Team = p.TeamAbbr,                                                   // Отбор
            Years = p.Years,                                                     // Години
            Height = p.Height,                                                   // Ръст
            Weight = p.Weight,                                                   // Тегло
            Id = p.PlayerCode,                                                   // Идентификатор
            Rookie = p.Rookie,                                                   // Новобранец ли е
            Grades = p.Grades.OrderBy(g => g.Slot).Select(g => g.Value).ToArray(), // 10-те оценки по ред
            RealStats = p.Ppg == null                                            // Без мачове: празен масив
                ? Array.Empty<double>()
                : new[] { p.Ppg.Value, p.Rpg ?? 0, p.Apg ?? 0, p.Spg ?? 0, p.Bpg ?? 0 }, // Иначе петте статистики
        }).ToList();
    }

    // Чете таблица QuizEntries и я превръща в играчи за куиза
    private static List<QuizPlayer> LoadQuizPlayers(AppDbContext db) =>
        db.QuizEntries.AsNoTracking().OrderBy(q => q.Id).ToList()   // Всички редове по реда на записване
            .Select(q => new QuizPlayer
            {
                Name = q.Name,                                                        // Име
                Positions = q.Positions.Split('/').ToList(),                          // Позиции
                Team = q.TeamAbbr,                                                    // Отбор
                Decade = q.Decade,                                                    // Десетилетие
                Overall = q.Overall,                                                  // Обща оценка
                Skills = new[] { q.JumpShot, q.Handles, q.Finishing, q.Defense, q.Size }, // 5-те умения
                Stats = new[] { q.Ppg, q.Rpg, q.Apg, q.Spg, q.Bpg },                  // 5-те статистики
            }).ToList();

    // Чете индекс на снимки: {"ключ": [номер на изображение, позиция в него]}
    private static Dictionary<string, int[]> LoadPhotos(string path) =>
        File.Exists(path)                                                                      // Ако файлът съществува
            ? JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(path)) ?? new() // четем го
            : new();                                                                           // иначе няма снимки

    // Превръща име в прост ключ: без ударения, малки букви, само латиница
    public static string NormalizeName(string name)
    {
        // Разделяме буквите от ударенията (например "č" става "c" + знак)
        var decomposed = name.Normalize(NormalizationForm.FormD);
        // Тук събираме само буквите, които ни трябват
        var sb = new StringBuilder();
        // Обхождаме всеки знак
        foreach (var ch in decomposed)
        {
            // Пропускаме знаците за ударение
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            // Превръщаме в малка буква
            var lower = char.ToLowerInvariant(ch);
            // Пазим само буквите a–z
            if (lower >= 'a' && lower <= 'z') sb.Append(lower);
        }
        // Връщаме готовия ключ
        return sb.ToString();
    }
}

// Зарежда всички данни от JSON файловете в папка Data при стартиране
using System.Globalization; // За нормализиране на имена (махане на ударения)
using System.Text;           // За работа с низове (StringBuilder, нормализация)
using System.Text.Json;      // За четене на JSON
using CourtVision.Models;    // Нашите модели на данни

namespace CourtVision.Services; // Пространство от имена за услугите

// Услуга, която държи всички данни в паметта
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

    // Конструктор: изпълнява се веднъж при стартиране на приложението
    public DataService(IWebHostEnvironment env)
    {
        // Пътят до папката с данни в проекта
        var dir = Path.Combine(env.ContentRootPath, "Data");
        // Зареждаме съставите за 82-0
        Pools = LoadPools(Path.Combine(dir, "pools.json"));
        // Запомняме ключовете в списък
        PoolKeys = Pools.Keys.ToList();
        // Зареждаме играчите за Build-a-Player
        BuildPlayers = LoadBuildPlayers(Path.Combine(dir, "build-players.json"));
        // Настройка: имената на свойствата в JSON не зависят от главни/малки букви
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        // Пътят до файла с играчите за куиза
        var quizFile = Path.Combine(dir, "quiz-players.json");
        // Зареждаме играчите за куиза (празен списък, ако файлът липсва)
        QuizPlayers = File.Exists(quizFile) ? JsonSerializer.Deserialize<List<QuizPlayer>>(File.ReadAllText(quizFile), options) ?? new() : new();
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

    // Чете pools.json: {"BOS|60": [[име, id, "SF/SG", ppg, rpg, apg, spg, bpg], ...]}
    private static Dictionary<string, List<PoolPlayer>> LoadPools(string path)
    {
        // Ако файлът липсва (играта още не е добавена), връщаме празен речник
        if (!File.Exists(path)) return new();
        // Отваряме JSON документа
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // Резултатът: празен речник
        var result = new Dictionary<string, List<PoolPlayer>>();
        // Обхождаме всеки състав (свойство на главния обект)
        foreach (var pool in doc.RootElement.EnumerateObject())
        {
            // Разделяме ключа на отбор и десетилетие
            var parts = pool.Name.Split('|');
            // Списък за играчите в този състав
            var players = new List<PoolPlayer>();
            // Обхождаме всеки ред (един играч)
            foreach (var row in pool.Value.EnumerateArray())
            {
                // Създаваме играча от стойностите в реда
                players.Add(new PoolPlayer
                {
                    Name = row[0].GetString() ?? "",                        // Име
                    Id = row[1].GetString() ?? "",                          // Идентификатор
                    Positions = (row[2].GetString() ?? "PG").Split('/').ToList(), // Позиции
                    Team = parts[0],                                        // Отбор от ключа
                    Decade = parts[1],                                      // Десетилетие от ключа
                    Ppg = row[3].GetDouble(),                               // Точки
                    Rpg = row[4].GetDouble(),                               // Борби
                    Apg = row[5].GetDouble(),                               // Асистенции
                    Spg = row[6].ValueKind == JsonValueKind.Null ? null : row[6].GetDouble(), // Кражби или null
                    Bpg = row[7].ValueKind == JsonValueKind.Null ? null : row[7].GetDouble(), // Блокади или null
                });
            }
            // Добавяме състава в резултата
            result[pool.Name] = players;
        }
        // Връщаме всички състави
        return result;
    }

    // Чете build-players.json: [[режим, група, име, отбор, години, ръст, тегло, id, новобранец, [оценки], [статистики]], ...]
    private static List<BuildPlayer> LoadBuildPlayers(string path)
    {
        // Ако файлът липсва, връщаме празен списък
        if (!File.Exists(path)) return new();
        // Отваряме JSON документа
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // Списък за резултата
        var list = new List<BuildPlayer>();
        // Брояч за поредния номер
        var index = 0;
        // Обхождаме всеки ред
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            // Създаваме играча
            list.Add(new BuildPlayer
            {
                Index = index++,                                                     // Пореден номер
                Mode = row[0].GetString() ?? "",                                     // Режим
                Group = row[1].GetString() ?? "",                                    // Група
                Name = row[2].GetString() ?? "",                                     // Име
                Team = row[3].GetString() ?? "",                                     // Отбор
                Years = row[4].GetString() ?? "",                                    // Години
                Height = row[5].GetInt32(),                                          // Ръст
                Weight = row[6].GetInt32(),                                          // Тегло
                Id = row[7].GetString() ?? "",                                       // Идентификатор
                Rookie = row[8].GetInt32() == 1,                                     // Новобранец ли е
                Grades = row[9].EnumerateArray().Select(g => g.GetInt32()).ToArray(),     // 10 оценки
                RealStats = row[10].EnumerateArray().Select(v => v.GetDouble()).ToArray(), // Реални статистики
            });
        }
        // Връщаме списъка
        return list;
    }

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

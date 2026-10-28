// Логиката на Higher or Lower: двама играчи, познай дали вторият има повече или по-малко от показателя
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Един показател: ключ, кратко име и пълно име за въпроса
public record HigherLowerStat(string Key, string Short, string Name);

// Услугата за Higher or Lower
public class HigherLowerService
{
    // Трите показателя, които се сравняват
    public static readonly HigherLowerStat[] Stats =
    {
        new("ppg", "PPG", "points per game"),     // Точки на мач
        new("rpg", "RPG", "rebounds per game"),   // Борби на мач
        new("apg", "APG", "assists per game"),    // Асистенции на мач
    };

    // Най-малката разлика между двамата играчи (за да няма почти равни стойности)
    private const double MinGap = 0.4;

    // All-Time: всички редове от съставите (един играч в един отбор и едно десетилетие)
    private readonly List<PoolPlayer> _allTime;
    // Current: днешните играчи със статистиките им от сезон 2025–26
    private readonly List<PoolPlayer> _current;

    // Конструктор: събира играчите за двата режима
    public HigherLowerService(DataService data)
    {
        // All-Time: от съставите на 82-0, само играчи с поне 4 точки на мач (по-познати имена)
        _allTime = data.Pools.Values.SelectMany(list => list).Where(p => p.Ppg >= 4).ToList();
        // Current: от Build-a-Player (режим Current), без новобранците, които още нямат мачове
        _current = data.BuildPlayers
            .Where(b => b.Mode == "c" && !b.Rookie && b.RealStats.Length == 5 && b.RealStats[0] >= 4) // Само с истински статистики и поне 4 точки
            .GroupBy(b => b.Name).Select(g => g.First())                                          // По един път за всяко име
            .Select(b => new PoolPlayer
            {
                Name = b.Name,                                              // Име
                Id = b.Id,                                                  // Идентификатор (за снимката)
                Positions = new List<string> { b.Group == "b" ? "C" : "SG" }, // Позиция само за цвета на рамката
                Team = b.Team,                                              // Отбор
                Decade = "20",                                              // Десетилетие (2020-те)
                Ppg = b.RealStats[0],                                       // Точки
                Rpg = b.RealStats[1],                                       // Борби
                Apg = b.RealStats[2],                                       // Асистенции
            })
            .ToList();
    }

    // Играчите за режим: "c" = Current, иначе All-Time
    private List<PoolPlayer> Pool(string mode) => mode == "c" ? _current : _allTime;

    // Броят играчи в режим (за менюто)
    public int Count(string mode) => Pool(mode).Count;

    // Играч по номер в даден режим
    public PoolPlayer Player(string mode, int index) => Pool(mode)[index];

    // Стойността на показателя за играч
    public static double Value(PoolPlayer p, string stat) => stat switch
    {
        "rpg" => p.Rpg,    // Борби
        "apg" => p.Apg,    // Асистенции
        _ => p.Ppg,        // Точки
    };

    // Информацията за показателя по ключ
    public static HigherLowerStat StatInfo(string key) => Stats.FirstOrDefault(s => s.Key == key) ?? Stats[0];

    // Случаен показател: точките се падат по-често (половината рундове)
    private static string RandomStat() => Random.Shared.NextDouble() < .5 ? "ppg" : Random.Shared.NextDouble() < .5 ? "rpg" : "apg";

    // Избира нов играч за дясната страна: друг човек и с достатъчна разлика в показателя
    private int PickRight(string mode, int left, string stat)
    {
        // Играчите в режима
        var players = Pool(mode);
        // Левият играч и стойността му
        var l = players[left];
        var lv = Value(l, stat);
        // Опитваме до 200 пъти да намерим подходящ
        for (var tries = 0; tries < 200; tries++)
        {
            // Случаен кандидат
            var i = Random.Shared.Next(players.Count);
            var p = players[i];
            // Трябва да е друг играч и разликата да е поне 0.4
            if (p.Id != l.Id && Math.Abs(Value(p, stat) - lv) >= MinGap) return i;
        }
        // Ако не намерим (почти невъзможно), взимаме всеки друг
        return (left + 1) % players.Count;
    }

    // Започва нова игра в даден режим (пазим най-добрите серии)
    public void Start(HigherLowerState s, string mode)
    {
        // Режимът: "c" (Current) или "a" (All-Time)
        s.Mode = mode == "c" ? "c" : "a";
        // Нулираме серията и отговора
        s.Score = 0; s.Guess = null; s.Correct = false; s.Over = false;
        // Нов показател и двама нови играчи
        s.Stat = RandomStat();
        s.Left = Random.Shared.Next(Pool(s.Mode).Count);
        s.Right = PickRight(s.Mode, s.Left, s.Stat);
    }

    // Отговор: "higher" (вторият има повече или равно) или "lower" (по-малко или равно)
    public void Guess(HigherLowerState s, string dir)
    {
        // Ако вече има отговор, играта е свършила или посоката е невалидна, спираме
        if (s.Mode == "" || s.Guess != null || s.Over || dir is not ("higher" or "lower")) return;
        // Двете стойности
        var lv = Value(Player(s.Mode, s.Left), s.Stat);
        var rv = Value(Player(s.Mode, s.Right), s.Stat);
        // Верен ли е отговорът (при равенство и двата са верни)
        s.Correct = dir == "higher" ? rv >= lv : rv <= lv;
        // Записваме отговора
        s.Guess = dir;
        // Верен: една точка повече и нов рекорд, ако е нужно
        if (s.Correct)
        {
            // Още един верен отговор
            s.Score++;
            // Рекордът за режима
            if (s.Mode == "c") s.BestCurrent = Math.Max(s.BestCurrent, s.Score); else s.BestAllTime = Math.Max(s.BestAllTime, s.Score);
        }
        // Грешен: край на играта
        else s.Over = true;
    }

    // Следващ рунд: десният играч минава вляво, идва нов вдясно и нов показател
    public void Next(HigherLowerState s)
    {
        // Може само след верен отговор
        if (s.Mode == "" || s.Guess == null || !s.Correct) return;
        // Десният става ляв
        s.Left = s.Right;
        // Нов показател и нов десен играч
        s.Stat = RandomStat();
        s.Right = PickRight(s.Mode, s.Left, s.Stat);
        // Изчистваме отговора
        s.Guess = null; s.Correct = false;
    }
}

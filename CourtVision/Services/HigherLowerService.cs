// Логиката на Higher or Lower: двама играчи, познай дали вторият има повече или по-малко от показателя
using CourtVision.Models;

namespace CourtVision.Services;

// Един показател: ключ, кратко име и пълно име за въпроса
public record HigherLowerStat(string Key, string Short, string Name);

// Услугата за Higher or Lower
public class HigherLowerService
{
    public static readonly HigherLowerStat[] Stats =
    {
        new("ppg", "PPG", "points per game"),
        new("rpg", "RPG", "rebounds per game"),
        new("apg", "APG", "assists per game"),
    };

    private const double MinGap = 0.4;

    private readonly List<PoolPlayer> _allTime;
    private readonly List<PoolPlayer> _current;

    // Конструктор: събира играчите за двата режима
    public HigherLowerService(DataService data)
    {
        _allTime = data.Pools.Values.SelectMany(list => list).Where(p => p.Ppg >= 4).ToList();
        _current = data.BuildPlayers
            .Where(b => b.Mode == "c" && !b.Rookie && b.RealStats.Length == 5 && b.RealStats[0] >= 4)
            .GroupBy(b => b.Name).Select(g => g.First())
            .Select(b => new PoolPlayer
            {
                Name = b.Name,
                Id = b.Id,
                Positions = new List<string> { b.Group == "b" ? "C" : "SG" },
                Team = b.Team,
                Decade = "20",
                Ppg = b.RealStats[0],
                Rpg = b.RealStats[1],
                Apg = b.RealStats[2],
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
        "rpg" => p.Rpg,
        "apg" => p.Apg,
        _ => p.Ppg,
    };

    // Информацията за показателя по ключ
    public static HigherLowerStat StatInfo(string key) => Stats.FirstOrDefault(s => s.Key == key) ?? Stats[0];

    // Случаен показател: точките се падат по-често (половината рундове)
    private static string RandomStat() => Random.Shared.NextDouble() < .5 ? "ppg" : Random.Shared.NextDouble() < .5 ? "rpg" : "apg";

    // Избира нов играч за дясната страна: друг човек и с достатъчна разлика в показателя
    private int PickRight(string mode, int left, string stat)
    {
        var players = Pool(mode);
        var l = players[left];
        var lv = Value(l, stat);
        for (var tries = 0; tries < 200; tries++)
        {
            var i = Random.Shared.Next(players.Count);
            var p = players[i];
            if (p.Id != l.Id && Math.Abs(Value(p, stat) - lv) >= MinGap) return i;
        }
        return (left + 1) % players.Count;
    }

    // Започва нова игра в даден режим (пазим най-добрите серии)
    public void Start(HigherLowerState s, string mode)
    {
        s.Mode = mode == "c" ? "c" : "a";
        s.Score = 0; s.Guess = null; s.Correct = false; s.Over = false;
        s.Stat = RandomStat();
        s.Left = Random.Shared.Next(Pool(s.Mode).Count);
        s.Right = PickRight(s.Mode, s.Left, s.Stat);
    }

    // Отговор: "higher" (вторият има повече или равно) или "lower" (по-малко или равно)
    public void Guess(HigherLowerState s, string dir)
    {
        if (s.Mode == "" || s.Guess != null || s.Over || dir is not ("higher" or "lower")) return;
        var lv = Value(Player(s.Mode, s.Left), s.Stat);
        var rv = Value(Player(s.Mode, s.Right), s.Stat);
        s.Correct = dir == "higher" ? rv >= lv : rv <= lv;
        s.Guess = dir;
        if (s.Correct)
        {
            s.Score++;
            if (s.Mode == "c") s.BestCurrent = Math.Max(s.BestCurrent, s.Score); else s.BestAllTime = Math.Max(s.BestAllTime, s.Score);
        }
        else s.Over = true;
    }

    // Бутонът Random: отговаря на случаен принцип, докато не сбърка (играта свършва)
    public void RandomPlay(HigherLowerState s)
    {
        if (s.Mode == "") Start(s, Random.Shared.Next(2) == 0 ? "c" : "a");
        for (var guard = 0; guard < 60 && !s.Over; guard++)
        {
            if (s.Guess != null && s.Correct) Next(s);
            Guess(s, Random.Shared.Next(2) == 0 ? "higher" : "lower");
        }
    }

    // Следващ рунд: десният играч минава вляво, идва нов вдясно и нов показател
    public void Next(HigherLowerState s)
    {
        if (s.Mode == "" || s.Guess == null || !s.Correct) return;
        s.Left = s.Right;
        s.Stat = RandomStat();
        s.Right = PickRight(s.Mode, s.Left, s.Stat);
        s.Guess = null; s.Correct = false;
    }
}

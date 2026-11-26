// Логиката на Guess the Player: само известни играчи (от 2000 г. насам и Залата на славата от 90-те), известен таен играч, 8 опита (и още, ако искаш) и подсказки за отбор, конференция, позиция, десетилетие и статистики
using CourtVision.Models;

namespace CourtVision.Services;

// Резултат от сравнение на едно поле: "hit" (зелено), "near" (жълто) или "miss" (сиво), плюс стрелка
public record GuessClue(string State, string Arrow);

// Услугата за Guess the Player
public class GuessService
{
    public const int MaxGuesses = 8;
    public const int ExtraGuesses = 4;
    private const double FamousCurrentGrade = 7.5;
    private const double GuessMinPpg = 8;
    private const double KnownPpg = 15;
    // От 90-те влизат само членовете на Залата на славата, които са играли тогава
    private static readonly HashSet<string> HallOfFame90s = new[]
    {
        "Michael Jordan", "Scottie Pippen", "Dennis Rodman", "Hakeem Olajuwon", "David Robinson",
        "Patrick Ewing", "Karl Malone", "John Stockton", "Charles Barkley", "Shaquille O'Neal",
        "Reggie Miller", "Gary Payton", "Clyde Drexler", "Alonzo Mourning", "Dikembe Mutombo",
        "Chris Mullin", "Mitch Richmond", "Tim Hardaway", "Grant Hill", "Jason Kidd",
        "Steve Nash", "Kevin Garnett", "Kobe Bryant", "Tim Duncan", "Allen Iverson",
        "Ray Allen", "Paul Pierce", "Vince Carter", "Dirk Nowitzki", "Chris Webber",
        "Ben Wallace", "Tracy McGrady", "Chauncey Billups", "Arvydas Sabonis", "Toni Kukoč",
        "Vlade Divac", "Šarūnas Marčiulionis", "Dražen Petrović", "Dominique Wilkins", "Joe Dumars",
        "Isiah Thomas", "Magic Johnson", "Larry Bird",
    }.Select(DataService.NormalizeName).ToHashSet();

    private readonly List<PoolPlayer> _players;
    private readonly List<int> _mysteries;
    private readonly Dictionary<string, int> _byName;

    // Конструктор: само играчи, които хората познават - от 2000 г. насам и членове на Залата на славата от 90-те
    public GuessService(DataService data)
    {
        var famous = data.BuildPlayers
            .Where(b => b.Mode == "a" || (b.Mode == "c" && b.AverageGrade >= FamousCurrentGrade))
            .Select(b => DataService.NormalizeName(b.Name))
            .Concat(HallOfFame90s)
            .ToHashSet();
        var lastSeason = data.BuildPlayers
            .Where(b => b.Mode == "c" && b.RealStats.Length == 5)
            .GroupBy(b => DataService.CurrentKey(b.Name))
            .ToDictionary(g => g.Key, g => g.First());
        _players = data.Pools.Values.SelectMany(list => list)
            .GroupBy(p => p.Id)
            .Select(g => Eligible(g.ToList()))
            .Where(lines => lines.Count > 0)
            .Where(lines =>
            {
                var top = lines.Max(p => p.Ppg);
                return top >= GuessMinPpg && (top >= KnownPpg || famous.Contains(DataService.NormalizeName(lines[0].Name)));
            })
            .Select(lines => Pick(lines, data.CurrentTeams, lastSeason))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
        // Тайният играч е винаги от известните
        _mysteries = Enumerable.Range(0, _players.Count)
            .Where(i => famous.Contains(DataService.NormalizeName(_players[i].Name)))
            .ToList();
        _byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _players.Count; i++) _byName[Label(i)] = i;
    }

    private static bool IsModern(string decade) => decade is "00" or "10" or "20";

    // Кои редове (отбор + десетилетие) на играча влизат в играта: от 2000 г. насам,
    // а за членовете на Залата на славата и 90-те. Играч, чийто най-добър сезон е преди 2000 г., отпада
    private static List<PoolPlayer> Eligible(List<PoolPlayer> lines)
    {
        if (HallOfFame90s.Contains(DataService.NormalizeName(lines[0].Name)))
            return lines.Where(p => p.Decade == "90" || IsModern(p.Decade)).ToList();
        var top = lines.OrderByDescending(p => p.Ppg).First();
        return IsModern(top.Decade) ? lines.Where(p => IsModern(p.Decade)).ToList() : new List<PoolPlayer>();
    }

    // Кой ред да показваме за играча: бившите играчи - с най-добрия си сезон;
    // сегашните (в състав за 2026–27) - с отбора, в който са сега, и статистиките от миналия сезон
    private static PoolPlayer Pick(List<PoolPlayer> lines, IReadOnlyDictionary<string, string> currentTeams, Dictionary<string, BuildPlayer> lastSeason)
    {
        var best = lines.OrderByDescending(p => p.Ppg).First();
        var recent = lines.Where(p => p.Decade == "20").OrderByDescending(p => p.Ppg).FirstOrDefault();
        var key = DataService.CurrentKey(best.Name);
        if (recent == null || !currentTeams.TryGetValue(key, out var team)) return best;
        var now = new PoolPlayer
        {
            Name = best.Name, Id = best.Id, Positions = recent.Positions, Team = team, Decade = "20",
            Ppg = recent.Ppg, Rpg = recent.Rpg, Apg = recent.Apg, Spg = recent.Spg, Bpg = recent.Bpg,
        };
        if (lastSeason.TryGetValue(key, out var b))
        {
            now.Ppg = b.RealStats[0]; now.Rpg = b.RealStats[1]; now.Apg = b.RealStats[2];
            now.Spg = b.RealStats[3]; now.Bpg = b.RealStats[4];
        }
        return now;
    }

    // Играч по номер
    public PoolPlayer Player(int index) => _players[index];

    public int Count => _players.Count;

    // Всички етикети (за подсказките докато пишеш)
    public List<string> Labels() => Enumerable.Range(0, _players.Count).Select(Label).ToList();

    // Етикетът за списъка с имена: само името, а при еднакви имена и отборът с десетилетието
    public string Label(int index)
    {
        var p = _players[index];
        var duplicate = (index > 0 && _players[index - 1].Name == p.Name) || (index < _players.Count - 1 && _players[index + 1].Name == p.Name);
        return duplicate ? $"{p.Name} ({p.Team} · {ChallengeService.EraYears(p.Decade)})" : p.Name;
    }

    // Започва нова игра със случаен таен играч
    public void Start(GuessState s)
    {
        s.Mystery = _mysteries[Random.Shared.Next(_mysteries.Count)];
        s.Guesses.Clear(); s.Won = false; s.Over = false; s.OutOfGuesses = false; s.Message = ""; s.Limit = MaxGuesses;
    }

    // Опит: името, което е написал играчът
    public void Guess(GuessState s, string? name)
    {
        if (s.Over || s.OutOfGuesses || s.Mystery < 0) return;
        if (string.IsNullOrWhiteSpace(name) || !_byName.TryGetValue(name.Trim(), out var index))
        {
            s.Message = "Player not found. Pick a name from the list.";
            return;
        }
        if (s.Guesses.Contains(index)) { s.Message = $"You already tried {_players[index].Name}."; return; }
        s.Guesses.Add(index);
        s.Message = "";
        if (index == s.Mystery) { s.Won = true; s.Over = true; s.Played++; s.Wins++; return; }
        if (s.Guesses.Count >= s.Limit) s.OutOfGuesses = true;
    }

    // Бутонът Random: прави един опит с известен играч, който има нещо общо с тайния (отбор или позиция), но не е той
    public void RandomPlay(GuessState s)
    {
        if (s.Mystery < 0) Start(s);
        if (s.Over || s.OutOfGuesses) return;
        var m = _players[s.Mystery];
        var free = _mysteries.Where(i => i != s.Mystery && !s.Guesses.Contains(i)).ToList();
        var sameTeam = free.Where(i => _players[i].Team == m.Team).ToList();
        var samePos = free.Where(i => _players[i].MainPosition == m.MainPosition
            && Teams.Get(_players[i].Team).IsEast == Teams.Get(m.Team).IsEast).ToList();
        var pick = sameTeam.Count > 0 ? sameTeam : samePos.Count > 0 ? samePos : free;
        if (pick.Count == 0) return;
        Guess(s, Label(pick[Random.Shared.Next(pick.Count)]));
    }

    // "Keep guessing": още 4 опита за същия таен играч
    public void More(GuessState s)
    {
        if (s.Over || !s.OutOfGuesses) return;
        s.Limit += ExtraGuesses; s.OutOfGuesses = false; s.Message = "";
    }

    // Отказ: показва отговора
    public void GiveUp(GuessState s)
    {
        if (s.Over || s.Mystery < 0) return;
        s.Over = true; s.OutOfGuesses = false; s.Played++; s.Message = "";
    }

    // ---------- Подсказки ----------

    // Отбор: зелено при същия отбор, жълто при същата конференция
    public static GuessClue TeamClue(PoolPlayer g, PoolPlayer m) =>
        g.Team == m.Team ? new("hit", "") : Teams.Get(g.Team).IsEast == Teams.Get(m.Team).IsEast ? new("near", "") : new("miss", "");

    // Конференция: зелено при същата
    public static GuessClue ConferenceClue(PoolPlayer g, PoolPlayer m) =>
        Teams.Get(g.Team).IsEast == Teams.Get(m.Team).IsEast ? new("hit", "") : new("miss", "");

    // Позиция: зелено при същата основна позиция, жълто, ако имат обща позиция
    public static GuessClue PositionClue(PoolPlayer g, PoolPlayer m) =>
        g.MainPosition == m.MainPosition ? new("hit", "") : g.Positions.Intersect(m.Positions).Any() ? new("near", "") : new("miss", "");

    // Номерът на десетилетието за сравнение (60 → 1960, 00 → 2000)
    private static int Year(string decade) => (string.Compare(decade, "50", StringComparison.Ordinal) >= 0 ? 1900 : 2000) + int.Parse(decade);

    // Десетилетие: зелено при същото, жълто при съседно; стрелката показва накъде е тайният играч
    public static GuessClue DecadeClue(PoolPlayer g, PoolPlayer m)
    {
        var diff = Year(m.Decade) - Year(g.Decade);
        var arrow = diff > 0 ? "↑" : diff < 0 ? "↓" : "";
        return new(diff == 0 ? "hit" : Math.Abs(diff) == 10 ? "near" : "miss", arrow);
    }

    // Статистика: зелено при разлика до 1, жълто до "near"; стрелката показва дали тайният има повече или по-малко
    public static GuessClue StatClue(double guess, double mystery, double near)
    {
        var diff = mystery - guess;
        var arrow = Math.Abs(diff) < 0.05 ? "" : diff > 0 ? "↑" : "↓";
        return new(Math.Abs(diff) <= 1 ? "hit" : Math.Abs(diff) <= near ? "near" : "miss", arrow);
    }
}

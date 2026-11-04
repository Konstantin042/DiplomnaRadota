// Логиката на Guess the Player: известен таен играч, 8 опита (и още, ако искаш) и подсказки за отбор, конференция, позиция, десетилетие и статистики
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
    private static readonly HashSet<string> Famous60s = new()
    {
        "Wilt Chamberlain", "Bill Russell", "Oscar Robertson", "Jerry West",
        "Elgin Baylor", "Rick Barry", "Willis Reed", "Bob Pettit",
    };

    private readonly List<PoolPlayer> _players;
    private readonly List<int> _mysteries;
    private readonly Dictionary<string, int> _byName;

    // Конструктор: събира играчите от съставите на 82-0
    public GuessService(DataService data)
    {
        _players = data.Pools.Values.SelectMany(list => list)
            .GroupBy(p => p.Id)
            .Select(g => g.OrderByDescending(p => p.Ppg).First())
            .Where(p => p.Ppg >= GuessMinPpg)
            .Where(p => p.Decade != "60" || Famous60s.Contains(p.Name))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
        var famous = data.BuildPlayers
            .Where(b => b.Mode == "a" || (b.Mode == "c" && b.AverageGrade >= FamousCurrentGrade))
            .Select(b => DataService.NormalizeName(b.Name))
            .ToHashSet();
        _mysteries = Enumerable.Range(0, _players.Count)
            .Where(i => famous.Contains(DataService.NormalizeName(_players[i].Name)) || Famous60s.Contains(_players[i].Name))
            .ToList();
        _byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _players.Count; i++) _byName[Label(i)] = i;
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

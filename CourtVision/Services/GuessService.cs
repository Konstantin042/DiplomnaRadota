// Логиката на Guess the Player: таен играч, 8 опита и подсказки за отбор, конференция, позиция, десетилетие и статистики
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Резултат от сравнение на едно поле: "hit" (зелено), "near" (жълто) или "miss" (сиво), плюс стрелка
public record GuessClue(string State, string Arrow);

// Услугата за Guess the Player
public class GuessService
{
    // Колко опита има играчът
    public const int MaxGuesses = 8;
    // Тайните играчи са само звезди (поне 18 точки на мач в най-добрия им отбор и десетилетие)
    private const double MysteryMinPpg = 18;
    // Опит може да е всеки играч с поне 8 точки на мач
    private const double GuessMinPpg = 8;

    // Всички играчи (по един ред за всеки човек: отборът и десетилетието с най-много точки)
    private readonly List<PoolPlayer> _players;
    // Номерата на играчите, които могат да са тайни
    private readonly List<int> _mysteries;
    // Име за търсене → номер на играча
    private readonly Dictionary<string, int> _byName;

    // Конструктор: събира играчите от съставите на 82-0
    public GuessService(DataService data)
    {
        // За всеки играч (по идентификатор) взимаме реда с най-много точки на мач
        _players = data.Pools.Values.SelectMany(list => list)
            .GroupBy(p => p.Id)                                      // Групираме по човек
            .Select(g => g.OrderByDescending(p => p.Ppg).First())    // Най-добрият му ред
            .Where(p => p.Ppg >= GuessMinPpg)                        // Само играчи с поне 8 точки
            .OrderBy(p => p.Name, StringComparer.Ordinal)            // Подредени по име
            .ToList();
        // Звездите, които могат да са тайни
        _mysteries = Enumerable.Range(0, _players.Count).Where(i => _players[i].Ppg >= MysteryMinPpg).ToList();
        // Речник за търсене по етикет (етикетът е уникален)
        _byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _players.Count; i++) _byName[Label(i)] = i;
    }

    // Играч по номер
    public PoolPlayer Player(int index) => _players[index];

    // Броят на всички играчи (за списъка с имена)
    public int Count => _players.Count;

    // Етикетът за списъка с имена: само името, а при еднакви имена и отборът с десетилетието
    public string Label(int index)
    {
        // Играчът
        var p = _players[index];
        // Има ли друг със същото име
        var duplicate = (index > 0 && _players[index - 1].Name == p.Name) || (index < _players.Count - 1 && _players[index + 1].Name == p.Name);
        // Етикетът
        return duplicate ? $"{p.Name} ({p.Team} · {ChallengeService.EraYears(p.Decade)})" : p.Name;
    }

    // Започва нова игра със случаен таен играч
    public void Start(GuessState s)
    {
        // Случаен таен играч от звездите
        s.Mystery = _mysteries[Random.Shared.Next(_mysteries.Count)];
        // Изчистваме опитите и съобщението
        s.Guesses.Clear(); s.Won = false; s.Over = false; s.Message = "";
    }

    // Опит: името, което е написал играчът
    public void Guess(GuessState s, string? name)
    {
        // Ако играта е свършила, нищо не правим
        if (s.Over || s.Mystery < 0) return;
        // Търсим играча по етикет
        if (string.IsNullOrWhiteSpace(name) || !_byName.TryGetValue(name.Trim(), out var index))
        {
            // Няма такъв играч в списъка
            s.Message = "Player not found. Pick a name from the list.";
            return;
        }
        // Вече опитан играч не се брои
        if (s.Guesses.Contains(index)) { s.Message = $"You already tried {_players[index].Name}."; return; }
        // Записваме опита
        s.Guesses.Add(index);
        s.Message = "";
        // Познали ли сме
        if (index == s.Mystery) { s.Won = true; s.Over = true; s.Played++; s.Wins++; return; }
        // Свършили ли са опитите
        if (s.Guesses.Count >= MaxGuesses) { s.Over = true; s.Played++; }
    }

    // Отказ: показва отговора
    public void GiveUp(GuessState s)
    {
        // Само ако играта още тече
        if (s.Over || s.Mystery < 0) return;
        // Край без победа
        s.Over = true; s.Played++; s.Message = "";
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
        // Разликата в години
        var diff = Year(m.Decade) - Year(g.Decade);
        // Стрелка: ↑ тайният е по-късно, ↓ по-рано
        var arrow = diff > 0 ? "↑" : diff < 0 ? "↓" : "";
        // Цвят
        return new(diff == 0 ? "hit" : Math.Abs(diff) == 10 ? "near" : "miss", arrow);
    }

    // Статистика: зелено при разлика до 1, жълто до "near"; стрелката показва дали тайният има повече или по-малко
    public static GuessClue StatClue(double guess, double mystery, double near)
    {
        // Разликата
        var diff = mystery - guess;
        // Стрелка: ↑ тайният има повече, ↓ по-малко
        var arrow = Math.Abs(diff) < 0.05 ? "" : diff > 0 ? "↑" : "↓";
        // Цвят
        return new(Math.Abs(diff) <= 1 ? "hit" : Math.Abs(diff) <= near ? "near" : "miss", arrow);
    }
}

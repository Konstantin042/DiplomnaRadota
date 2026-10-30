// Логиката на Guess the Player: известен таен играч, 8 опита (и още, ако искаш) и подсказки за отбор, конференция, позиция, десетилетие и статистики
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Резултат от сравнение на едно поле: "hit" (зелено), "near" (жълто) или "miss" (сиво), плюс стрелка
public record GuessClue(string State, string Arrow);

// Услугата за Guess the Player
public class GuessService
{
    // Колко опита има играчът в началото
    public const int MaxGuesses = 8;
    // Колко опита се добавят при "Keep guessing"
    public const int ExtraGuesses = 4;
    // Днешните играчи стават тайни само ако са звезди (средна оценка поне 7.5 от 10)
    private const double FamousCurrentGrade = 7.5;
    // Опит може да е всеки играч с поне 8 точки на мач
    private const double GuessMinPpg = 8;
    // От 60-те години остават само най-известните играчи (останалите са твърде непознати)
    private static readonly HashSet<string> Famous60s = new()
    {
        "Wilt Chamberlain", "Bill Russell", "Oscar Robertson", "Jerry West", // Четирите най-големи звезди на десетилетието
        "Elgin Baylor", "Rick Barry", "Willis Reed", "Bob Pettit",           // И още четири легенди от Залата на славата
    };

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
            .Where(p => p.Decade != "60" || Famous60s.Contains(p.Name)) // От 60-те само най-известните
            .OrderBy(p => p.Name, StringComparer.Ordinal)            // Подредени по име
            .ToList();
        // Известните играчи: всички от All-Time списъка на Build-a-Player и най-добрите днешни играчи
        var famous = data.BuildPlayers
            .Where(b => b.Mode == "a" || (b.Mode == "c" && b.AverageGrade >= FamousCurrentGrade)) // All-Time или днешна звезда
            .Select(b => DataService.NormalizeName(b.Name))                                     // Името без ударения и интервали
            .ToHashSet();
        // Тайните играчи са само известни играчи (плюс легендите от 60-те)
        _mysteries = Enumerable.Range(0, _players.Count)
            .Where(i => famous.Contains(DataService.NormalizeName(_players[i].Name)) || Famous60s.Contains(_players[i].Name))
            .ToList();
        // Речник за търсене по етикет (етикетът е уникален)
        _byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _players.Count; i++) _byName[Label(i)] = i;
    }

    // Играч по номер
    public PoolPlayer Player(int index) => _players[index];

    // Броят на всички играчи (за списъка с имена)
    public int Count => _players.Count;

    // Всички етикети (за подсказките докато пишеш)
    public List<string> Labels() => Enumerable.Range(0, _players.Count).Select(Label).ToList();

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
        // Изчистваме опитите, съобщението и броя опити
        s.Guesses.Clear(); s.Won = false; s.Over = false; s.OutOfGuesses = false; s.Message = ""; s.Limit = MaxGuesses;
    }

    // Опит: името, което е написал играчът
    public void Guess(GuessState s, string? name)
    {
        // Ако играта е свършила или опитите са свършили, нищо не правим
        if (s.Over || s.OutOfGuesses || s.Mystery < 0) return;
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
        // Свършили ли са опитите: отговорът остава скрит, играчът избира дали да продължи
        if (s.Guesses.Count >= s.Limit) s.OutOfGuesses = true;
    }

    // "Keep guessing": още 4 опита за същия таен играч
    public void More(GuessState s)
    {
        // Само когато опитите са свършили
        if (s.Over || !s.OutOfGuesses) return;
        // Добавяме опити и продължаваме
        s.Limit += ExtraGuesses; s.OutOfGuesses = false; s.Message = "";
    }

    // Отказ: показва отговора
    public void GiveUp(GuessState s)
    {
        // Само ако играта още тече
        if (s.Over || s.Mystery < 0) return;
        // Край без победа
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

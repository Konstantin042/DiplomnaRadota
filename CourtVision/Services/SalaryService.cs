// Логиката на Salary Cap Game (по идеята на Build-a-Player Salary): мрежа 5×5 с цени, таван на заплатите, разбъркване, скаут и заключване на билда
using System.Text.Json;
using CourtVision.Models;

namespace CourtVision.Services;

// Една колона (категория) в мрежата: ключ, име, цвят и кои атрибути на гард/висок играч я описват
public record SalaryColumn(string Key, string Label, string Color, int[] GuardAttrs, int[] BigAttrs);

// Услугата за Salary Cap Game
public class SalaryService
{
    public static readonly int[] Prices = { 50, 40, 30, 20, 10 };
    public static readonly int[] BudgetOptions = { 130, 140, 150, 160, 170 };
    public const int ShufflesPerGrid = 2;
    public const int ScoutsPerGrid = 1;

    public static readonly SalaryColumn[] Columns =
    {
        new("finishing", "FINISHING", "#fb923c", new[] { 1 }, new[] { 1 }),
        new("shooting", "SHOOTING", "#34d399", new[] { 0 }, new[] { 0 }),
        new("defense", "DEFENSE", "#38bdf8", new[] { 4 }, new[] { 4 }),
        new("playmaking", "PLAYMAKING", "#60a5fa", new[] { 2, 3 }, new[] { 3 }),
        new("size", "SIZE & ATHLETICISM", "#a78bfa", new[] { 5, 6, 7 }, new[] { 7, 6 }),
    };

    // Колко процента от класирането отиват във всеки ред: $50M = най-добрите 20%, $40M = следващите 20% и т.н.
    private static readonly (double Lo, double Hi)[] Bands = { (0, .2), (.2, .4), (.4, .6), (.6, .8), (.8, 1) };

    private readonly DataService _data;
    private readonly List<BuildPlayer> _regulars;
    private readonly List<BuildPlayer> _legends;
    private readonly Dictionary<string, string> _photos;

    // Конструктор: подготвя играчите и снимките веднъж при стартиране
    public SalaryService(DataService data, IWebHostEnvironment env)
    {
        _data = data;
        var file = Path.Combine(env.ContentRootPath, "Data", "salary-photos.json");
        _photos = File.Exists(file) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? new() : new();
        _regulars = data.BuildPlayers
            .Where(p => p.Mode == "c" && p.AverageGrade >= 3)
            .GroupBy(p => DataService.NormalizeName(p.Name))
            .Select(g => g.First())
            .ToList();
        var current = _regulars.Select(p => DataService.NormalizeName(p.Name)).ToHashSet();
        _legends = data.BuildPlayers
            .Where(p => p.Mode == "a" && p.AverageGrade >= 3)
            .Where(p => !current.Contains(DataService.NormalizeName(p.Name)))
            .Where(p => _photos.ContainsKey(DataService.NormalizeName(p.Name)))
            .GroupBy(p => DataService.NormalizeName(p.Name))
            .Select(g => g.OrderByDescending(p => p.AverageGrade).First())
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
    }

    // Играч по пореден номер
    public BuildPlayer Player(int index) => _data.BuildPlayers[index];

    // Адресът на снимката на играч (или null, ако няма)
    public string? PhotoUrl(BuildPlayer p) =>
        _photos.TryGetValue(DataService.NormalizeName(p.Name), out var id) ? $"/faces/salary/{id}.webp" : null;

    // Адресът на голямата снимка по име (ползва се и от Higher or Lower)
    public string? PhotoUrl(string name) =>
        _photos.TryGetValue(DataService.NormalizeName(name), out var id) ? $"/faces/salary/{id}.webp" : null;

    // Номерата на атрибутите, които описват колоната за този играч (гард или висок)
    private static int[] AttrsFor(BuildPlayer p, SalaryColumn col) => p.Group == "b" ? col.BigAttrs : col.GuardAttrs;

    // Оценката на играча в колоната: средното от атрибутите ѝ (0–11)
    public static double CatScore(BuildPlayer p, SalaryColumn col) => AttrsFor(p, col).Average(i => p.Grades[i]);

    // Най-силният атрибут на играча в колоната (за общата оценка)
    private static int PeakScore(BuildPlayer p, SalaryColumn col) => AttrsFor(p, col).Max(i => p.Grades[i]);

    // ---------- Дати ----------

    // Ключът на днешния ден, например "2026-10-27"
    public static string TodayKey() => DateTime.Today.ToString("yyyy-MM-dd");

    // Последните 10 дни (за менюто с дати), от днес назад
    public static List<DateTime> RecentDates() => Enumerable.Range(0, 10).Select(i => DateTime.Today.AddDays(-i)).ToList();

    // Превръща дата в число за генератора, например 20261027
    private static int SeedOf(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

    // Таванът на заплатите за дадено число (еднакъв за всички в един ден)
    private static int BudgetFor(int seed) => BudgetOptions[new Random(seed ^ 0xB0B0B0).Next(BudgetOptions.Length)];

    // ---------- Създаване на мрежата ----------

    // Създава нов ден (или безкрайна игра) с мрежа, таван и power-ups
    public SalaryDay NewDay(string key)
    {
        var date = key == "inf" ? DateTime.Today : DateTime.ParseExact(key, "yyyy-MM-dd", null);
        var seed = key == "inf" ? Random.Shared.Next(1, int.MaxValue) : SeedOf(date);
        var yesterday = key == "inf" ? new HashSet<int>() : GridNames(MakeGrid(SeedOf(date.AddDays(-1)), date.AddDays(-1), new HashSet<int>()));
        return new SalaryDay
        {
            Key = key,
            Seed = seed,
            Budget = BudgetFor(seed),
            Grid = MakeGrid(seed, key == "inf" ? date.AddDays(Random.Shared.Next(1, 3650)) : date, yesterday),
            Shuffles = ShufflesPerGrid,
            Scouts = ScoutsPerGrid,
        };
    }

    // Всички играчи в мрежа (като множество от номера)
    private static HashSet<int> GridNames(List<List<int>> grid) => grid.SelectMany(c => c).ToHashSet();

    // В кой ред трябва да е легендата в колоната: според мястото ѝ сред днешните играчи
    private int LegendTier(BuildPlayer legend, SalaryColumn col)
    {
        var score = CatScore(legend, col);
        var sorted = _regulars.Select(p => CatScore(p, col)).OrderByDescending(x => x).ToList();
        var rank = sorted.FindIndex(x => x <= score);
        var pct = rank == -1 ? 1.0 : rank / (double)sorted.Count;
        return pct < .2 ? 0 : pct < .4 ? 1 : pct < .6 ? 2 : pct < .8 ? 3 : 4;
    }

    // Генерира мрежата: за всяка колона по един играч за всяка цена, плюс две легенди
    private List<List<int>> MakeGrid(int seed, DateTime date, HashSet<int> penalized)
    {
        var rand = new Random(seed);
        var dayIndex = (int)(date - new DateTime(2026, 1, 1)).TotalDays;
        var order = new List<BuildPlayer>(_legends);
        var fixedRand = new Random(0x0BADC0FE);
        for (var i = order.Count - 1; i > 0; i--)
        {
            var j = fixedRand.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        var legendSlots = new Dictionary<(int Col, int Row), int>();
        if (order.Count >= 2)
        {
            var n = order.Count;
            var leg0 = order[((dayIndex * 2) % n + n) % n];
            var leg1 = order[((dayIndex * 2 + 1) % n + n) % n];
            var c0 = rand.Next(Columns.Length);
            var c1 = rand.Next(Columns.Length);
            var t0 = LegendTier(leg0, Columns[c0]);
            var t1 = LegendTier(leg1, Columns[c1]);
            if (c0 == c1 && t0 == t1)
            {
                int Shift(int t) => t < Prices.Length - 1 ? t + 1 : t - 1;
                if (CatScore(leg0, Columns[c0]) <= CatScore(leg1, Columns[c0])) t0 = Shift(t0); else t1 = Shift(t1);
            }
            legendSlots[(c0, t0)] = leg0.Index;
            legendSlots[(c1, t1)] = leg1.Index;
        }
        var used = legendSlots.Values.ToHashSet();
        var grid = new List<List<int>>();
        for (var ci = 0; ci < Columns.Length; ci++)
        {
            var col = Columns[ci];
            var scored = _regulars.OrderByDescending(p => CatScore(p, col) - (penalized.Contains(p.Index) ? 3.5 : 0)).ThenBy(p => p.Index).ToList();
            var count = scored.Count;
            var column = new List<int>();
            for (var ti = 0; ti < Prices.Length; ti++)
            {
                if (legendSlots.TryGetValue((ci, ti), out var legend)) { column.Add(legend); continue; }
                var (lo, hi) = Bands[ti];
                var start = (int)Math.Floor(lo * count);
                var end = Math.Max(start + 1, (int)Math.Floor(hi * count));
                var pool = scored.Skip(start).Take(end - start).Where(p => !used.Contains(p.Index)).ToList();
                if (pool.Count == 0) pool = scored.Where(p => !used.Contains(p.Index)).ToList();
                if (pool.Count == 0) pool = scored;
                var pick = pool[rand.Next(pool.Count)];
                column.Add(pick.Index);
                used.Add(pick.Index);
            }
            grid.Add(column);
        }
        return grid;
    }

    // ---------- Действия на играча ----------

    // Похарчените милиони
    public static int Spent(SalaryDay d) => d.Picks.Values.Sum(row => Prices[row]);

    // Истина, ако във всяка колона има избран играч
    public static bool AllPicked(SalaryDay d) => d.Picks.Count == Columns.Length;

    // Натискане на играч: избира го, маха избора или го скаутва (в режим скаут)
    public void Tap(SalaryDay d, int col, int row)
    {
        if (col < 0 || col >= Columns.Length || row < 0 || row >= Prices.Length || d.Result != null) return;
        d.Flip.Clear();
        if (d.Mode == "scout") { Scout(d, col, row); return; }
        if (d.Mode == "shuffle") { d.Mode = ""; return; }
        if (d.Picks.TryGetValue(col, out var current) && current == row) d.Picks.Remove(col); else d.Picks[col] = row;
    }

    // Скаут: показва оценката на играча в колоната му (буква от F до S)
    private void Scout(SalaryDay d, int col, int row)
    {
        if (d.Scouts <= 0) { d.Mode = ""; return; }
        var p = Player(d.Grid[col][row]);
        if (d.Scouted.ContainsKey(p.Index)) { d.Mode = ""; return; }
        d.Scouted[p.Index] = Grades.Letter(CatScore(p, Columns[col]));
        d.Scouts--;
        d.Mode = "";
    }

    // Бутонът Random: проверява всички 3125 комбинации под тавана и взима случайна от петте с най-висок OVR
    public void RandomPicks(SalaryDay d)
    {
        if (d.Result != null) return;
        var options = new List<(int[] Rows, double Score)>();
        var rows = new int[Columns.Length];
        for (var code = 0; code < (int)Math.Pow(Prices.Length, Columns.Length); code++)
        {
            var n = code;
            for (var c = 0; c < Columns.Length; c++) { rows[c] = n % Prices.Length; n /= Prices.Length; }
            if (rows.Sum(r => Prices[r]) > d.Budget) continue;
            // същата оценка като при подписване (OVR), а средната оценка в колоната решава при равенство
            var score = Enumerable.Range(0, Columns.Length).Sum(c =>
            {
                var p = Player(d.Grid[c][rows[c]]);
                return PeakScore(p, Columns[c]) + 0.1 * CatScore(p, Columns[c]);
            });
            options.Add(((int[])rows.Clone(), score));
        }
        if (options.Count == 0) return;
        var top = options.OrderByDescending(o => o.Score).Take(5).ToList();
        var pick = top[Random.Shared.Next(top.Count)].Rows;
        d.Picks = Enumerable.Range(0, Columns.Length).ToDictionary(c => c, c => pick[c]);
        d.Mode = "";
        d.Flip.Clear();
    }

    // Включва/изключва режим разбъркване или скаут
    public static void ToggleMode(SalaryDay d, string mode)
    {
        if (d.Result != null) return;
        if (mode == "shuffle" && d.Shuffles <= 0) return;
        if (mode == "scout" && d.Scouts <= 0) return;
        d.Mode = d.Mode == mode ? "" : mode is "shuffle" or "scout" ? mode : "";
        d.Flip.Clear();
    }

    // Разбърква списък така, че никой елемент да не остане на мястото си
    private static List<int> Derange(List<int> items)
    {
        var result = new List<int>(items);
        do
        {
            for (var i = result.Count - 1; i > 0; i--)
            {
                var j = Random.Shared.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
        } while (result.Where((x, i) => x == items[i]).Any());
        return result;
    }

    // Разбъркване на колона (⇅): играчите в колоната сменят цените си
    public static void ShuffleColumn(SalaryDay d, int col)
    {
        if (d.Mode != "shuffle" || d.Shuffles <= 0 || d.Result != null || col < 0 || col >= Columns.Length) return;
        d.Grid[col] = Derange(d.Grid[col]);
        d.Picks.Remove(col);
        d.Flip = Enumerable.Range(0, Prices.Length).Select(r => $"{col}-{r}").ToList();
        d.Shuffles--;
        d.Mode = "";
    }

    // Разбъркване на ред (⇄): играчите с една и съща цена сменят колоните си
    public static void ShuffleRow(SalaryDay d, int row)
    {
        if (d.Mode != "shuffle" || d.Shuffles <= 0 || d.Result != null || row < 0 || row >= Prices.Length) return;
        var players = d.Grid.Select(c => c[row]).ToList();
        var shuffled = Derange(players);
        for (var c = 0; c < Columns.Length; c++) d.Grid[c][row] = shuffled[c];
        foreach (var c in d.Picks.Where(p => p.Value == row).Select(p => p.Key).ToList()) d.Picks.Remove(c);
        d.Flip = Enumerable.Range(0, Columns.Length).Select(c => $"{c}-{row}").ToList();
        d.Shuffles--;
        d.Mode = "";
    }

    // ---------- Заключване на билда ----------

    // Изчислява резултата: сбор от най-силния атрибут на всеки избран играч и статистики на мач
    public SalaryResult Score(SalaryDay d)
    {
        int Peak(int c) => PeakScore(Player(d.Grid[c][d.Picks[c]]), Columns[c]);
        int f = Peak(0), s = Peak(1), df = Peak(2), pm = Peak(3), sz = Peak(4);
        return new SalaryResult
        {
            Players = Enumerable.Range(0, Columns.Length).Select(c => d.Grid[c][d.Picks[c]]).ToList(),
            Prices = Enumerable.Range(0, Columns.Length).Select(c => Prices[d.Picks[c]]).ToList(),
            Overall = f + s + df + pm + sz,
            Ppg = Math.Round((f + s) / 2.0 * 2.6 + 1.8, 1),
            Apg = Math.Round(pm * 0.8 + 0.5, 1),
            Rpg = Math.Round(sz * 1.0 + 1.2, 1),
            Spent = Spent(d),
        };
    }

    // Стойността на атрибут с даден ключ при играч (ако го няма, взимаме най-близкия)
    private static int Value(BuildPlayer p, string key)
    {
        var keys = Grades.For(p.Group).Select(a => a.Key).ToList();
        var k = keys.IndexOf(key);
        if (k >= 0) return p.Grades[k];
        var alternatives = key switch
        {
            "passing" or "handles" => new[] { "playmaking" },
            "playmaking" => new[] { "passing", "handles" },
            "perimeterDefense" => new[] { "interiorDefense" },
            "interiorDefense" => new[] { "perimeterDefense" },
            _ => Array.Empty<string>(),
        };
        var values = alternatives.Select(a => keys.IndexOf(a)).Where(i => i >= 0).Select(i => p.Grades[i]).ToList();
        return values.Count > 0 ? (int)Math.Round(values.Average()) : 5;
    }

    // От кой избран играч (по колона) идва атрибутът с този ключ; -1 = средно от всички
    private static int SourceColumn(string key) => key switch
    {
        "finishing" => 0,
        "jumpShot" => 1,
        "perimeterDefense" or "interiorDefense" => 2,
        "passing" or "handles" or "playmaking" => 3,
        "speed" or "bounce" or "size" => 4,
        _ => -1,
    };

    // Създава билд (10 атрибута) от петте избрани играчи, за да изиграем сезон
    public BuildState MakeBuild(SalaryDay d)
    {
        var picks = Enumerable.Range(0, Columns.Length).Select(c => Player(d.Grid[c][d.Picks[c]])).ToList();
        var group = picks[4].Group == "b" ? "b" : "g";
        var build = new BuildState { Mode = "c", Group = group };
        var attrs = Grades.For(group);
        for (var i = 0; i < attrs.Length; i++)
        {
            var key = attrs[i].Key;
            var source = SourceColumn(key);
            if (source >= 0)
            {
                build.Taken[i] = new TakenAttribute { PlayerIndex = picks[source].Index, Value = Value(picks[source], key) };
                continue;
            }
            var values = picks.Where(p => Grades.For(p.Group).Any(a => a.Key == key)).Select(p => Value(p, key)).ToList();
            var average = values.Count > 0 ? (int)Math.Round(values.Average()) : 5;
            build.Taken[i] = new TakenAttribute { PlayerIndex = picks[4].Index, Value = average };
        }
        return build;
    }

    // Отборът, за който играе билдът (за дневната игра е еднакъв за всички в деня)
    public static string TeamFor(SalaryDay d) =>
        d.Key == "inf"
            ? Teams.All[Random.Shared.Next(Teams.All.Count)].Abbr
            : Teams.All[new Random(d.Seed * 7 + 3).Next(Teams.All.Count)].Abbr;
}

// Логиката на Salary Cap Game (по идеята на Build-a-Player Salary): мрежа 5×5 с цени, таван на заплатите, разбъркване, скаут и заключване на билда
using System.Text.Json;   // За четене на файла със снимките
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Една колона (категория) в мрежата: ключ, име, цвят и кои атрибути на гард/висок играч я описват
public record SalaryColumn(string Key, string Label, string Color, int[] GuardAttrs, int[] BigAttrs);

// Услугата за Salary Cap Game
public class SalaryService
{
    // Цените на петте реда (в милиони долари), от най-скъпия към най-евтиния
    public static readonly int[] Prices = { 50, 40, 30, 20, 10 };
    // Възможните тавани на заплатите; всеки ден се пада един от тях
    public static readonly int[] BudgetOptions = { 130, 140, 150, 160, 170 };
    // Броят разбърквания (rerolls) за една мрежа
    public const int ShufflesPerGrid = 2;
    // Броят скаутвания за една мрежа
    public const int ScoutsPerGrid = 1;

    // Петте колони; числата са номерата на атрибутите в Grades.Guard / Grades.Big
    public static readonly SalaryColumn[] Columns =
    {
        new("finishing", "FINISHING", "#fb923c", new[] { 1 }, new[] { 1 }),                  // Завършване до коша
        new("shooting", "SHOOTING", "#34d399", new[] { 0 }, new[] { 0 }),                    // Стрелба
        new("defense", "DEFENSE", "#38bdf8", new[] { 4 }, new[] { 4 }),                      // Защита (периметър за гардове, кош за високи)
        new("playmaking", "PLAYMAKING", "#60a5fa", new[] { 2, 3 }, new[] { 3 }),             // Създаване на игра (подаване и дрибъл за гардове)
        new("size", "SIZE & ATHLETICISM", "#a78bfa", new[] { 5, 6, 7 }, new[] { 7, 6 }),     // Ръст и атлетизъм
    };

    // Колко процента от класирането отиват във всеки ред: $50M = най-добрите 20%, $40M = следващите 20% и т.н.
    private static readonly (double Lo, double Hi)[] Bands = { (0, .2), (.2, .4), (.4, .6), (.6, .8), (.8, 1) };

    // Всички данни
    private readonly DataService _data;
    // Днешните играчи (без много слабите), от които се пълни мрежата
    private readonly List<BuildPlayer> _regulars;
    // Легендите (бивши звезди със снимка); всеки ден в мрежата има две
    private readonly List<BuildPlayer> _legends;
    // Снимките: нормализирано име → номер на файла в wwwroot/faces/salary
    private readonly Dictionary<string, string> _photos;

    // Конструктор: подготвя играчите и снимките веднъж при стартиране
    public SalaryService(DataService data, IWebHostEnvironment env)
    {
        // Запомняме данните
        _data = data;
        // Пътят до файла със снимките
        var file = Path.Combine(env.ContentRootPath, "Data", "salary-photos.json");
        // Четем го (празен речник, ако липсва)
        _photos = File.Exists(file) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? new() : new();
        // Днешните играчи със средна оценка поне 3 (D+), по един път за всяко име
        _regulars = data.BuildPlayers
            .Where(p => p.Mode == "c" && p.AverageGrade >= 3)           // Само днешни и не много слаби
            .GroupBy(p => DataService.NormalizeName(p.Name))            // Групираме по име
            .Select(g => g.First())                                     // Взимаме по един
            .ToList();
        // Имената на днешните играчи (за да не ги броим и за легенди)
        var current = _regulars.Select(p => DataService.NormalizeName(p.Name)).ToHashSet();
        // Легендите: играчи от режима All-Time, които вече не играят и имат снимка
        _legends = data.BuildPlayers
            .Where(p => p.Mode == "a" && p.AverageGrade >= 3)                         // Само All-Time и не много слаби
            .Where(p => !current.Contains(DataService.NormalizeName(p.Name)))         // Без днешните играчи
            .Where(p => _photos.ContainsKey(DataService.NormalizeName(p.Name)))       // Само със снимка
            .GroupBy(p => DataService.NormalizeName(p.Name))                          // По един път за всяко име
            .Select(g => g.OrderByDescending(p => p.AverageGrade).First())            // Най-силната му версия (с най-добрия му отбор)
            .OrderBy(p => p.Name, StringComparer.Ordinal)                             // Постоянен ред
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
        // Безкрайна игра: случайно число; иначе числото на датата
        var date = key == "inf" ? DateTime.Today : DateTime.ParseExact(key, "yyyy-MM-dd", null);
        var seed = key == "inf" ? Random.Shared.Next(1, int.MaxValue) : SeedOf(date);
        // При дневната игра играчите от вчерашната мрежа се падат по-рядко
        var yesterday = key == "inf" ? new HashSet<int>() : GridNames(MakeGrid(SeedOf(date.AddDays(-1)), date.AddDays(-1), new HashSet<int>()));
        // Връщаме новия ден
        return new SalaryDay
        {
            Key = key,                                   // Ключ
            Seed = seed,                                 // Число за генератора
            Budget = BudgetFor(seed),                    // Таван
            Grid = MakeGrid(seed, key == "inf" ? date.AddDays(Random.Shared.Next(1, 3650)) : date, yesterday), // Мрежата 5×5 (безкрайната игра взима случайни легенди)
            Shuffles = ShufflesPerGrid,                  // 2 разбърквания
            Scouts = ScoutsPerGrid,                      // 1 скаут
        };
    }

    // Всички играчи в мрежа (като множество от номера)
    private static HashSet<int> GridNames(List<List<int>> grid) => grid.SelectMany(c => c).ToHashSet();

    // В кой ред трябва да е легендата в колоната: според мястото ѝ сред днешните играчи
    private int LegendTier(BuildPlayer legend, SalaryColumn col)
    {
        // Оценката на легендата
        var score = CatScore(legend, col);
        // Оценките на днешните играчи от най-висока към най-ниска
        var sorted = _regulars.Select(p => CatScore(p, col)).OrderByDescending(x => x).ToList();
        // Първото място, където днешен играч е по-слаб или равен на легендата
        var rank = sorted.FindIndex(x => x <= score);
        // Процент от класирането (1 = най-слабата)
        var pct = rank == -1 ? 1.0 : rank / (double)sorted.Count;
        // Ред според процента
        return pct < .2 ? 0 : pct < .4 ? 1 : pct < .6 ? 2 : pct < .8 ? 3 : 4;
    }

    // Генерира мрежата: за всяка колона по един играч за всяка цена, плюс две легенди
    private List<List<int>> MakeGrid(int seed, DateTime date, HashSet<int> penalized)
    {
        // Генератор на случайни числа, еднакъв за едно и също число
        var rand = new Random(seed);
        // Номерът на деня от 1 януари 2026 (легендите се редуват по него)
        var dayIndex = (int)(date - new DateTime(2026, 1, 1)).TotalDays;
        // Разбъркваме легендите винаги по един и същи начин
        var order = new List<BuildPlayer>(_legends);
        var fixedRand = new Random(0x0BADC0FE);
        for (var i = order.Count - 1; i > 0; i--)
        {
            // Размяна на два елемента (Fisher–Yates)
            var j = fixedRand.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        // Мястото на легендите: колона → (ред, номер на играча)
        var legendSlots = new Dictionary<(int Col, int Row), int>();
        // Ако има поне две легенди, слагаме две
        if (order.Count >= 2)
        {
            // Двете легенди за деня (всички се падат, преди някоя да се повтори)
            var n = order.Count;
            var leg0 = order[((dayIndex * 2) % n + n) % n];
            var leg1 = order[((dayIndex * 2 + 1) % n + n) % n];
            // Случайни колони
            var c0 = rand.Next(Columns.Length);
            var c1 = rand.Next(Columns.Length);
            // Редът според силата им
            var t0 = LegendTier(leg0, Columns[c0]);
            var t1 = LegendTier(leg1, Columns[c1]);
            // Ако попаднат на едно и също място, по-слабата отива един ред по-евтино
            if (c0 == c1 && t0 == t1)
            {
                // Помощна функция: съседен ред (към по-евтиния, освен ако вече е най-евтиният)
                int Shift(int t) => t < Prices.Length - 1 ? t + 1 : t - 1;
                if (CatScore(leg0, Columns[c0]) <= CatScore(leg1, Columns[c0])) t0 = Shift(t0); else t1 = Shift(t1);
            }
            // Записваме местата
            legendSlots[(c0, t0)] = leg0.Index;
            legendSlots[(c1, t1)] = leg1.Index;
        }
        // Вече използваните играчи (никой не се появява два пъти)
        var used = legendSlots.Values.ToHashSet();
        // Резултатът: 5 колони по 5 реда
        var grid = new List<List<int>>();
        for (var ci = 0; ci < Columns.Length; ci++)
        {
            // Колоната
            var col = Columns[ci];
            // Днешните играчи, подредени по оценка в колоната (вчерашните са с наказание 3.5)
            var scored = _regulars.OrderByDescending(p => CatScore(p, col) - (penalized.Contains(p.Index) ? 3.5 : 0)).ThenBy(p => p.Index).ToList();
            // Броят им
            var count = scored.Count;
            // Редовете в тази колона
            var column = new List<int>();
            for (var ti = 0; ti < Prices.Length; ti++)
            {
                // Ако тук е легенда, я слагаме
                if (legendSlots.TryGetValue((ci, ti), out var legend)) { column.Add(legend); continue; }
                // Частта от класирането за тази цена
                var (lo, hi) = Bands[ti];
                var start = (int)Math.Floor(lo * count);
                var end = Math.Max(start + 1, (int)Math.Floor(hi * count));
                // Кандидатите от тази част, които още не са използвани
                var pool = scored.Skip(start).Take(end - start).Where(p => !used.Contains(p.Index)).ToList();
                // Ако няма, взимаме от всички неизползвани (и накрая от всички)
                if (pool.Count == 0) pool = scored.Where(p => !used.Contains(p.Index)).ToList();
                if (pool.Count == 0) pool = scored;
                // Случаен кандидат
                var pick = pool[rand.Next(pool.Count)];
                // Записваме го и го маркираме като използван
                column.Add(pick.Index);
                used.Add(pick.Index);
            }
            // Добавяме колоната
            grid.Add(column);
        }
        // Връщаме мрежата
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
        // Невалидна клетка или заключен билд: нищо не правим
        if (col < 0 || col >= Columns.Length || row < 0 || row >= Prices.Length || d.Result != null) return;
        // Изчистваме старата анимация
        d.Flip.Clear();
        // В режим скаут показваме оценката на играча
        if (d.Mode == "scout") { Scout(d, col, row); return; }
        // В режим разбъркване натискането на играч отменя режима
        if (d.Mode == "shuffle") { d.Mode = ""; return; }
        // Ако този играч вече е избран, махаме избора; иначе го избираме (замества другия в колоната)
        if (d.Picks.TryGetValue(col, out var current) && current == row) d.Picks.Remove(col); else d.Picks[col] = row;
    }

    // Скаут: показва оценката на играча в колоната му (буква от F до S)
    private void Scout(SalaryDay d, int col, int row)
    {
        // Няма оставащи скаутвания
        if (d.Scouts <= 0) { d.Mode = ""; return; }
        // Играчът в клетката
        var p = Player(d.Grid[col][row]);
        // Ако вече е скаутван, не харчим скаутването
        if (d.Scouted.ContainsKey(p.Index)) { d.Mode = ""; return; }
        // Записваме буквата
        d.Scouted[p.Index] = Grades.Letter(CatScore(p, Columns[col]));
        // Използвахме скаутването
        d.Scouts--;
        // Връщаме се в обикновен режим
        d.Mode = "";
    }

    // Включва/изключва режим разбъркване или скаут
    public static void ToggleMode(SalaryDay d, string mode)
    {
        // Заключен билд: нищо
        if (d.Result != null) return;
        // Без оставащи power-ups не може
        if (mode == "shuffle" && d.Shuffles <= 0) return;
        if (mode == "scout" && d.Scouts <= 0) return;
        // Повторно натискане изключва режима
        d.Mode = d.Mode == mode ? "" : mode is "shuffle" or "scout" ? mode : "";
        // Изчистваме анимацията
        d.Flip.Clear();
    }

    // Разбърква списък така, че никой елемент да не остане на мястото си
    private static List<int> Derange(List<int> items)
    {
        // Копие за разбъркване
        var result = new List<int>(items);
        // Повтаряме, докато никой не е на старото си място
        do
        {
            for (var i = result.Count - 1; i > 0; i--)
            {
                var j = Random.Shared.Next(i + 1);           // Случайна позиция
                (result[i], result[j]) = (result[j], result[i]); // Размяна
            }
        } while (result.Where((x, i) => x == items[i]).Any());
        // Връщаме разбъркания списък
        return result;
    }

    // Разбъркване на колона (⇅): играчите в колоната сменят цените си
    public static void ShuffleColumn(SalaryDay d, int col)
    {
        // Трябва да сме в режим разбъркване, с оставащо разбъркване и валидна колона
        if (d.Mode != "shuffle" || d.Shuffles <= 0 || d.Result != null || col < 0 || col >= Columns.Length) return;
        // Нов ред на играчите в колоната
        d.Grid[col] = Derange(d.Grid[col]);
        // Изборът в тази колона вече не важи
        d.Picks.Remove(col);
        // Анимираме всички клетки в колоната
        d.Flip = Enumerable.Range(0, Prices.Length).Select(r => $"{col}-{r}").ToList();
        // Използвахме разбъркване и излизаме от режима
        d.Shuffles--;
        d.Mode = "";
    }

    // Разбъркване на ред (⇄): играчите с една и съща цена сменят колоните си
    public static void ShuffleRow(SalaryDay d, int row)
    {
        // Трябва да сме в режим разбъркване, с оставащо разбъркване и валиден ред
        if (d.Mode != "shuffle" || d.Shuffles <= 0 || d.Result != null || row < 0 || row >= Prices.Length) return;
        // Играчите в реда (по един от всяка колона)
        var players = d.Grid.Select(c => c[row]).ToList();
        // Нов ред
        var shuffled = Derange(players);
        // Слагаме всеки в новата му колона
        for (var c = 0; c < Columns.Length; c++) d.Grid[c][row] = shuffled[c];
        // Изборите в този ред вече не важат
        foreach (var c in d.Picks.Where(p => p.Value == row).Select(p => p.Key).ToList()) d.Picks.Remove(c);
        // Анимираме всички клетки в реда
        d.Flip = Enumerable.Range(0, Columns.Length).Select(c => $"{c}-{row}").ToList();
        // Използвахме разбъркване и излизаме от режима
        d.Shuffles--;
        d.Mode = "";
    }

    // ---------- Заключване на билда ----------

    // Изчислява резултата: сбор от най-силния атрибут на всеки избран играч и статистики на мач
    public SalaryResult Score(SalaryDay d)
    {
        // Най-силният атрибут на играча в колона c
        int Peak(int c) => PeakScore(Player(d.Grid[c][d.Picks[c]]), Columns[c]);
        // Петте стойности: завършване, стрелба, защита, създаване, ръст
        int f = Peak(0), s = Peak(1), df = Peak(2), pm = Peak(3), sz = Peak(4);
        // Резултатът
        return new SalaryResult
        {
            Players = Enumerable.Range(0, Columns.Length).Select(c => d.Grid[c][d.Picks[c]]).ToList(), // Избраните играчи
            Prices = Enumerable.Range(0, Columns.Length).Select(c => Prices[d.Picks[c]]).ToList(),     // Цените им
            Overall = f + s + df + pm + sz,                                // Обща оценка (до 55)
            Ppg = Math.Round((f + s) / 2.0 * 2.6 + 1.8, 1),                // Точки: от завършване и стрелба
            Apg = Math.Round(pm * 0.8 + 0.5, 1),                           // Асистенции: от създаване на игра
            Rpg = Math.Round(sz * 1.0 + 1.2, 1),                           // Борби: от ръст и атлетизъм
            Spent = Spent(d),                                              // Похарчено
        };
    }

    // Стойността на атрибут с даден ключ при играч (ако го няма, взимаме най-близкия)
    private static int Value(BuildPlayer p, string key)
    {
        // Ключовете на атрибутите за групата на играча
        var keys = Grades.For(p.Group).Select(a => a.Key).ToList();
        // Ако играчът има този атрибут, го връщаме
        var k = keys.IndexOf(key);
        if (k >= 0) return p.Grades[k];
        // Иначе търсим заместител: подаване/дрибъл ↔ създаване на игра, защита на периметъра ↔ защита на коша
        var alternatives = key switch
        {
            "passing" or "handles" => new[] { "playmaking" },
            "playmaking" => new[] { "passing", "handles" },
            "perimeterDefense" => new[] { "interiorDefense" },
            "interiorDefense" => new[] { "perimeterDefense" },
            _ => Array.Empty<string>(),
        };
        // Стойностите на заместителите
        var values = alternatives.Select(a => keys.IndexOf(a)).Where(i => i >= 0).Select(i => p.Grades[i]).ToList();
        // Средното от тях или 5 (C), ако няма
        return values.Count > 0 ? (int)Math.Round(values.Average()) : 5;
    }

    // От кой избран играч (по колона) идва атрибутът с този ключ; -1 = средно от всички
    private static int SourceColumn(string key) => key switch
    {
        "finishing" => 0,                                       // Завършване
        "jumpShot" => 1,                                        // Стрелба
        "perimeterDefense" or "interiorDefense" => 2,           // Защита
        "passing" or "handles" or "playmaking" => 3,            // Създаване на игра
        "speed" or "bounce" or "size" => 4,                     // Ръст и атлетизъм
        _ => -1,                                                // IQ, лидерство и борба: средно от петимата
    };

    // Създава билд (10 атрибута) от петте избрани играчи, за да изиграем сезон
    public BuildState MakeBuild(SalaryDay d)
    {
        // Избраните играчи по колони
        var picks = Enumerable.Range(0, Columns.Length).Select(c => Player(d.Grid[c][d.Picks[c]])).ToList();
        // Позицията на билда идва от играча за ръст и атлетизъм
        var group = picks[4].Group == "b" ? "b" : "g";
        // Новият билд
        var build = new BuildState { Mode = "c", Group = group };
        // Атрибутите на групата
        var attrs = Grades.For(group);
        for (var i = 0; i < attrs.Length; i++)
        {
            // Ключът на атрибута
            var key = attrs[i].Key;
            // От коя колона идва
            var source = SourceColumn(key);
            // Ако е от конкретен играч, взимаме неговата стойност
            if (source >= 0)
            {
                build.Taken[i] = new TakenAttribute { PlayerIndex = picks[source].Index, Value = Value(picks[source], key) };
                continue;
            }
            // Иначе средното от играчите, които имат този атрибут (борбата я имат само високите)
            var values = picks.Where(p => Grades.For(p.Group).Any(a => a.Key == key)).Select(p => Value(p, key)).ToList();
            var average = values.Count > 0 ? (int)Math.Round(values.Average()) : 5;
            // Записваме го с играча за ръст като "източник"
            build.Taken[i] = new TakenAttribute { PlayerIndex = picks[4].Index, Value = average };
        }
        // Връщаме готовия билд
        return build;
    }

    // Отборът, за който играе билдът (за дневната игра е еднакъв за всички в деня)
    public static string TeamFor(SalaryDay d) =>
        d.Key == "inf"
            ? Teams.All[Random.Shared.Next(Teams.All.Count)].Abbr          // Безкрайна игра: случаен отбор
            : Teams.All[new Random(d.Seed * 7 + 3).Next(Teams.All.Count)].Abbr; // Дневна игра: отбор според датата
}

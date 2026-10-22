// Симулация на сезона: 82 мача, класиране, награди, Play-In, плейофи и финали
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Услугата за сезона
public class SeasonService
{
    // Имената на кръговете в плейофите
    public static readonly string[] RoundNames = { "First Round", "Conference Semifinals", "Conference Finals", "NBA Finals" };

    // Всички данни
    private readonly DataService _data;
    // Силата на всеки отбор според днешните му играчи
    private readonly Dictionary<string, double> _baseStrength;

    // Конструктор: изчислява силата на отборите веднъж
    public SeasonService(DataService data)
    {
        // Запомняме данните
        _data = data;
        // Тук пишем силата на всеки отбор
        _baseStrength = new Dictionary<string, double>();
        // Обхождаме всички отбори
        foreach (var team in Teams.All)
        {
            // Средните оценки на 8-те най-добри днешни играчи на отбора
            var best = CurrentPlayers(team.Abbr).Take(8).Select(p => p.AverageGrade).ToList();
            // Ако няма играчи, слагаме средна оценка 5
            var avg = best.Count > 0 ? best.Average() : 5;
            // Сила = 62 + средна оценка × 3.2 (около 76–82)
            _baseStrength[team.Abbr] = 62 + avg * 3.2;
        }
    }

    // Днешните играчи на отбор, подредени от най-добрия
    public List<BuildPlayer> CurrentPlayers(string team) =>
        _data.BuildPlayers.Where(p => p.Mode == "c" && p.Team == team).OrderByDescending(p => p.AverageGrade).ToList();

    // Шанс отбор със сила a да бие отбор със сила b (home = предимство на домакина)
    public static double WinChance(double a, double b, double home = 0) =>
        Math.Clamp(1 / (1 + Math.Exp(-(a - b) / 5.5)) + home, 0.08, 0.9);

    // Оценки за нападение и защита на отбор (с нашия играч, ако е подаден билд)
    public (double Off, double Def) TeamGrades(string team, BuildState? build)
    {
        // 8-те най-добри играчи
        var players = CurrentPlayers(team).Take(8).ToList();
        // Нападение: средно от стрелба, завършване и двете умения за създаване
        var off = players.Count > 0 ? players.Average(p => (p.Grades[0] + p.Grades[1] + p.Grades[2] + p.Grades[3]) / 4.0) : 5;
        // Защита: средно от защита, ръст и IQ
        var def = players.Count > 0 ? players.Average(p => (p.Grades[4] + p.Grades[7] + p.Grades[8]) / 3.0) : 5;
        // Ако има билд, добавяме нашия играч с по-голямо тегло
        if (build != null)
        {
            // Оценките на билда
            var v = BuildService.Values(build).Select(x => (double)(x ?? 0)).ToArray();
            // Смесваме: 7 части отбор, 2 части нашия играч
            off = (off * 7 + (v[0] + v[1] + v[2] + v[3]) / 4 * 2) / 9;
            def = (def * 7 + (v[4] + v[7] + v[8]) / 3 * 2) / 9;
        }
        // Връщаме двете оценки
        return (off, def);
    }

    // Създава целия сезон за избрания отбор
    public SeasonState Start(BuildState build, string team)
    {
        // OVR на нашия играч
        var ovr = BuildService.Overall(build) ?? 60;
        // Нашата и другата конференция
        var conf = Teams.Get(team).IsEast ? Teams.East : Teams.West;
        var other = Teams.Get(team).IsEast ? Teams.West : Teams.East;
        // Силата на всички отбори, като нашият става по-силен с добрия играч
        var strength = new Dictionary<string, double>(_baseStrength) { [team] = _baseStrength[team] + (ovr - 80) * 0.38 };
        // Средната сила в лигата
        var average = _baseStrength.Values.Average();
        // Противниците ни (всички без нас)
        var opponents = Teams.All.Select(t => t.Abbr).Where(t => t != team).ToList();
        // Нашите 82 мача
        var games = new List<SeasonGame>();
        for (var i = 0; i < 82; i++)
        {
            // Противникът (разбъркано между всички)
            var opp = opponents[(i * 7) % opponents.Count];
            // Домакинство на всеки втори мач
            var home = i % 2 == 1 ? 0.04 : -0.04;
            // Мачът с резултат
            games.Add(new SeasonGame { Opponent = opp, Win = Random.Shared.NextDouble() < WinChance(strength[team], strength[opp], home) });
        }
        // Победите на всеки отбор
        var wins = new Dictionary<string, int>();
        foreach (var t in Teams.All.Select(x => x.Abbr))
        {
            // Нашите победи са от нашите мачове
            if (t == team) { wins[t] = games.Count(g => g.Win); continue; }
            // Другите играят 82 мача срещу "среден" отбор
            var p = WinChance(strength[t], average);
            wins[t] = Enumerable.Range(0, 82).Count(_ => Random.Shared.NextDouble() < p);
        }
        // Класиране: повече победи е по-напред, при равенство по-силният
        List<string> Standing(IEnumerable<string> list) =>
            list.OrderByDescending(t => wins[t]).ThenByDescending(t => strength[t]).ToList();
        // Създаваме състоянието на сезона
        var state = new SeasonState
        {
            Team = team,                                            // Нашият отбор
            Mode = build.Mode,                                      // Режим
            Group = build.Group,                                    // Група
            Overall = ovr,                                          // OVR
            Archetype = BuildService.Archetype(build),              // Тип играч
            JerseyNumber = Random.Shared.Next(1, 100),              // Случаен номер на фланелката
            Games = games,                                          // 82-та мача
            Wins = wins,                                            // Победите на всички
            Strength = strength,                                    // Силата на всички
            Standings = Standing(conf),                             // Класиране в нашата конференция
            OtherStandings = Standing(other),                       // Класиране в другата
            Stats = BuildService.SeasonStats(build),                // Статистиките ни
            Build = Grades.For(build.Group).Select((a, i) => new BuildLine
            {
                Attribute = a.Label,                                            // Име на атрибута
                Player = _data.BuildPlayers[build.Taken[i].PlayerIndex].Name,   // От кого е копиран
                PlayerId = _data.BuildPlayers[build.Taken[i].PlayerIndex].Id,   // За снимката
                Value = build.Taken[i].Value,                                   // Оценката
            }).ToList(),
        };
        // Изчисляваме наградите
        SetAwards(state, build);
        // Изчисляваме и плейофите още сега (показват се по-късно)
        PreparePlayoffs(state);
        // Връщаме готовия сезон
        return state;
    }

    // Изчислява MVP и DPOY: нашият играч срещу най-добрите реални кандидати
    private void SetAwards(SeasonState s, BuildState build)
    {
        // Нашите статистики
        var r = s.Stats;
        // Кандидатите: днешни играчи с реални статистики
        var candidates = _data.BuildPlayers.Where(p => p.Mode == "c" && !p.Rookie && p.RealStats.Length == 5).ToList();
        // Най-добрият кандидат за MVP: статистики + победи на отбора
        var best = candidates.OrderByDescending(p => MvpScore(p.RealStats[0], p.RealStats[1], p.RealStats[2], s.Wins[p.Team])).First();
        // Нашият резултат (с бонус за OVR 95+)
        var my = MvpScore(r.Ppg, r.Rpg, r.Apg, s.Wins[s.Team]) + (s.Overall >= 95 ? 6 : 0);
        // MVP е нашият играч, ако резултатът ни е по-добър
        s.Mvp = my > MvpScore(best.RealStats[0], best.RealStats[1], best.RealStats[2], s.Wins[best.Team])
            ? new Award { IsYou = true, Name = "Your build", Team = s.Team, Line = $"{r.Ppg:0.0} PPG · {r.Rpg:0.0} RPG · {r.Apg:0.0} APG" }
            : new Award { Name = best.Name, Team = best.Team, Id = best.Id, Line = $"{best.RealStats[0]:0.0} PPG · {best.RealStats[1]:0.0} RPG · {best.RealStats[2]:0.0} APG" };
        // Нашата защита (атрибутът за защита е на позиция 4)
        var myDefense = BuildService.Values(build)[4] ?? 0;
        // Най-добрият защитник сред кандидатите
        var dbest = candidates.OrderByDescending(p => p.RealStats[3] * 4 + p.RealStats[4] * 4 + p.Grades[4] * .6).First();
        // Сравняваме нашия играч с него
        s.Dpoy = r.Spg * 4 + r.Bpg * 4 + myDefense * .6 > dbest.RealStats[3] * 4 + dbest.RealStats[4] * 4 + dbest.Grades[4] * .6
            ? new Award { IsYou = true, Name = "Your build", Team = s.Team, Line = $"{r.Spg:0.0} SPG · {r.Bpg:0.0} BPG" }
            : new Award { Name = dbest.Name, Team = dbest.Team, Id = dbest.Id, Line = $"{dbest.RealStats[3]:0.0} SPG · {dbest.RealStats[4]:0.0} BPG" };
    }

    // Резултат за MVP: точки + 0.8 × борби + 1.1 × асистенции + 0.35 × победи
    private static double MvpScore(double ppg, double rpg, double apg, int wins) => ppg + rpg * .8 + apg * 1.1 + wins * .35;

    // Резултат от един мач: победителят печели с 1 до 16 точки
    private static (int Winner, int Loser) Score()
    {
        // Основа около 104 точки
        var loser = 104 + (int)Math.Round((Random.Shared.NextDouble() - .5) * 22);
        // Разлика от 1 до 16
        var margin = 1 + Random.Shared.Next(16);
        // Връщаме двата резултата
        return (loser + margin, loser);
    }

    // Добавя мач към серия: кой е победил и с какъв резултат
    private static void AddGame(Series series, bool aWins)
    {
        // Генерираме резултат
        var (w, l) = Score();
        // Записваме победителя
        series.Games.Add(aWins);
        // Точките на A и B
        series.ScoresA.Add(aWins ? w : l);
        series.ScoresB.Add(aWins ? l : w);
    }

    // Серия до 4 победи: домакинство 2-2-1-1-1 за по-високо поставения (A)
    private static Series PlaySeries(SeasonState s, string a, string b)
    {
        // Кой мач е домакинство за A
        var home = new[] { true, true, false, false, true, false, true };
        // Нова серия
        var series = new Series { A = a, B = b };
        // Играем, докато някой стигне 4 победи
        for (var k = 0; series.WinsA < 4 && series.WinsB < 4; k++)
            // Шанс за победа с предимство на домакина
            AddGame(series, Random.Shared.NextDouble() < WinChance(s.Strength[a], s.Strength[b], home[k] ? .05 : -.05));
        // Връщаме серията
        return series;
    }

    // Един мач (Play-In)
    private static Series PlayOne(SeasonState s, string a, string b)
    {
        // Серия с един мач
        var series = new Series { A = a, B = b, Single = true };
        // A е домакин
        AddGame(series, Random.Shared.NextDouble() < WinChance(s.Strength[a], s.Strength[b], .04));
        // Връщаме мача
        return series;
    }

    // Play-In: 7 срещу 8, 9 срещу 10, после загубилият от 7–8 срещу победителя от 9–10
    private static PlayInResult PlayIn(SeasonState s, List<string> st)
    {
        // Мач 7 срещу 8
        var m78 = PlayOne(s, st[6], st[7]);
        // Мач 9 срещу 10
        var m910 = PlayOne(s, st[8], st[9]);
        // Мач за последното място
        var m8 = PlayOne(s, m78.Loser, m910.Winner);
        // Резултатът: местата 1–6, победителят от 7–8 и победителят от последния мач
        return new PlayInResult
        {
            Seeds = st.Take(6).Append(m78.Winner).Append(m8.Winner).ToList(),
            Games = new List<PlayInGame>
            {
                new() { Label = "7 vs 8", Match = m78 },     // Първи мач
                new() { Label = "9 vs 10", Match = m910 },   // Втори мач
                new() { Label = "Final spot", Match = m8 },  // Решаващ мач
            },
        };
    }

    // Плейофна схема за една конференция
    private static Bracket MakeBracket(SeasonState s, List<string> seeds)
    {
        // Помощна функция: номерът на поставяне
        int Seed(string t) => seeds.IndexOf(t) + 1;
        // Подрежда двама: по-високо поставеният е първи
        (string, string) Order(string x, string y) => Seed(x) < Seed(y) ? (x, y) : (y, x);
        // Нова схема
        var b = new Bracket { Seeds = seeds };
        // Първи кръг: 1–8, 4–5, 3–6, 2–7
        foreach (var (x, y) in new[] { (1, 8), (4, 5), (3, 6), (2, 7) })
            b.FirstRound.Add(PlaySeries(s, seeds[x - 1], seeds[y - 1]));
        // Полуфинали: победителите от 1–8 и 4–5, после от 3–6 и 2–7
        var (a1, b1) = Order(b.FirstRound[0].Winner, b.FirstRound[1].Winner);
        b.Semis.Add(PlaySeries(s, a1, b1));
        var (a2, b2) = Order(b.FirstRound[2].Winner, b.FirstRound[3].Winner);
        b.Semis.Add(PlaySeries(s, a2, b2));
        // Финал на конференцията
        var (a3, b3) = Order(b.Semis[0].Winner, b.Semis[1].Winner);
        b.ConferenceFinal = PlaySeries(s, a3, b3);
        // Връщаме схемата
        return b;
    }

    // Изчислява Play-In, плейофите, финалите и нашия път
    private static void PreparePlayoffs(SeasonState s)
    {
        // Нашето място
        var seed = s.Seed;
        // Play-In за нашата конференция, ако сме 7–10
        s.PlayIn = seed >= 7 && seed <= 10 ? PlayIn(s, s.Standings) : null;
        // Play-In за другата конференция (винаги)
        var otherPlayIn = PlayIn(s, s.OtherStandings);
        // Поставените 8 в нашата конференция
        var mySeeds = s.PlayIn?.Seeds ?? s.Standings.Take(8).ToList();
        // Схемите на двете конференции
        s.MyBracket = MakeBracket(s, mySeeds);
        s.OtherBracket = MakeBracket(s, otherPlayIn.Seeds);
        // Финалите: отборът с повече победи е домакин
        var champs = new[] { s.MyBracket.Champion, s.OtherBracket.Champion }.OrderByDescending(t => s.Wins[t]).ToArray();
        s.Finals = PlaySeries(s, champs[0], champs[1]);
        // Нашият път
        s.Path = new List<PathRound>();
        // Мачовете ни в Play-In
        if (s.PlayIn != null)
            foreach (var g in s.PlayIn.Games.Where(g => g.Match.Has(s.Team)))
                s.Path.Add(new PathRound { Name = "Play-In · " + g.Label, Round = -1, Match = g.Match });
        // Ако не сме в плейофите, пътят свършва тук
        if (mySeeds.Contains(s.Team))
        {
            // Първи кръг
            var r1 = s.MyBracket.FirstRound.First(x => x.Has(s.Team));
            s.Path.Add(new PathRound { Name = RoundNames[0], Round = 0, Match = r1 });
            // Ако сме спечелили, полуфинал
            if (r1.Winner == s.Team)
            {
                var sf = s.MyBracket.Semis.First(x => x.Has(s.Team));
                s.Path.Add(new PathRound { Name = RoundNames[1], Round = 1, Match = sf });
                // Ако сме спечелили, финал на конференцията
                if (sf.Winner == s.Team)
                {
                    s.Path.Add(new PathRound { Name = RoundNames[2], Round = 2, Match = s.MyBracket.ConferenceFinal });
                    // Ако сме спечелили, финалите на NBA
                    if (s.MyBracket.Champion == s.Team)
                        s.Path.Add(new PathRound { Name = RoundNames[3], Round = 3, Match = s.Finals });
                }
            }
        }
        // Отбелязваме, че плейофите са изчислени
        s.Prepared = true;
    }

    // Текст за края на сезона
    public static string Outcome(SeasonState s)
    {
        // Последният кръг, който сме играли
        var last = s.Path.LastOrDefault();
        // Не сме играли нищо: извън първите 10
        if (last == null) return "Missed the playoffs";
        // Спечелили сме финалите
        if (last.Round == 3 && last.Match.Winner == s.Team) return "NBA Champions!";
        // Отпаднали сме в Play-In
        if (last.Round < 0) return "Eliminated in the Play-In";
        // Отпаднали сме в някой кръг
        return "Lost in the " + last.Name;
    }
}

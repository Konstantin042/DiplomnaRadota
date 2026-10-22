// Логиката на Build-a-Player: завъртане на отбор и играч, копиране на атрибути, OVR и тип на играча
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Услугата за Build-a-Player
public class BuildService
{
    // Всички данни
    private readonly DataService _data;

    // Конструктор: получава данните
    public BuildService(DataService data) => _data = data;

    // Всички играчи за избрания режим и група
    public List<BuildPlayer> Pool(BuildState s) =>
        _data.BuildPlayers.Where(p => p.Mode == s.Mode && p.Group == s.Group).ToList();

    // Брой играчи за даден режим и група (за началния екран)
    public int Count(string mode, string group) =>
        _data.BuildPlayers.Count(p => p.Mode == mode && p.Group == group);

    // Играч по пореден номер
    public BuildPlayer Player(int index) => _data.BuildPlayers[index];

    // Неизползваните играчи, групирани по отбор
    public Dictionary<string, List<BuildPlayer>> FreeByTeam(BuildState s) =>
        Pool(s).Where(p => !s.Used.Contains(p.Index))   // Махаме използваните
               .GroupBy(p => p.Team)                    // Групираме по отбор
               .ToDictionary(g => g.Key, g => g.ToList()); // Превръщаме в речник

    // Започва нов билд в избрания режим
    public void Start(BuildState s, string mode)
    {
        // Пазим групата (Guard/Big), всичко останало започва отначало
        var group = s.Group;
        // Нулираме състоянието
        s.Mode = mode; s.Group = group; s.Tab = "skills"; s.Team = null; s.Current = null;
        // Връщаме респиновете
        s.TeamRespins = 1; s.PlayerRespins = 1;
        // Изчистваме атрибутите и използваните играчи
        s.Taken.Clear(); s.Used.Clear();
    }

    // Завъртане: kind = "all" (нов отбор и играч), "team" (респин на отбор), "player" (респин на играч)
    public void Spin(BuildState s, string kind)
    {
        // След като билдът е готов, няма завъртане
        if (s.Done) return;
        // Свободните играчи по отбори
        var byTeam = FreeByTeam(s);
        // Ако няма никой, спираме
        if (byTeam.Count == 0) return;
        // Респин на играч: същият отбор, друг играч
        if (kind == "player")
        {
            // Трябва да има респин, отбор и поне още един играч в него
            if (s.PlayerRespins <= 0 || s.Team == null || !byTeam.TryGetValue(s.Team, out var mates) || mates.Count < 2) return;
            // Намаляваме респиновете
            s.PlayerRespins--;
            // Избираме друг играч от същия отбор
            var others = mates.Where(p => p.Index != s.Current).ToList();
            s.Current = others[Random.Shared.Next(others.Count)].Index;
            return;
        }
        // Респин на отбор: намаляваме, ако има
        if (kind == "team")
        {
            // Без оставащ респин не може
            if (s.TeamRespins <= 0) return;
            s.TeamRespins--;
        }
        // Списък с отборите (без текущия, ако има други)
        var teams = byTeam.Keys.Where(t => t != s.Team || byTeam.Count == 1).ToList();
        // Случаен отбор
        s.Team = teams[Random.Shared.Next(teams.Count)];
        // Случаен играч от него
        var players = byTeam[s.Team];
        s.Current = players[Random.Shared.Next(players.Count)].Index;
    }

    // Копира атрибута с даден номер от завъртания играч
    public void Take(BuildState s, int attribute)
    {
        // Трябва да има играч, атрибутът да е свободен и номерът да е валиден
        if (s.Current == null || s.Taken.ContainsKey(attribute) || attribute < 0 || attribute > 9) return;
        // Завъртаният играч
        var player = Player(s.Current.Value);
        // Записваме оценката му за този атрибут
        s.Taken[attribute] = new TakenAttribute { PlayerIndex = player.Index, Value = player.Grades[attribute] };
        // Играчът вече е използван
        s.Used.Add(player.Index);
        // Изчистваме завъртания играч
        s.Current = null;
        // Отваряме таба на атрибута, който току-що попълнихме
        if (!s.Done) s.Tab = Grades.TabOf(attribute);
    }

    // Оценките на нашия играч (null за празните атрибути)
    public static int?[] Values(BuildState s) =>
        Enumerable.Range(0, 10).Select(i => s.Taken.TryGetValue(i, out var t) ? t.Value : (int?)null).ToArray();

    // Обща оценка OVR от 40 до 99 (средно от попълнените атрибути)
    public static int? Overall(BuildState s)
    {
        // Само попълнените оценки
        var values = s.Taken.Values.Select(t => t.Value).ToList();
        // Ако няма нищо, няма OVR
        if (values.Count == 0) return null;
        // Средната оценка (0–11) превърната в 40–99
        return Math.Min(99, (int)Math.Round(40 + values.Average() * 5.4));
    }

    // Тип на играча според най-силните му атрибути, например "All-Star Floor General"
    public static string Archetype(BuildState s)
    {
        // Всички оценки (празните са 0)
        var v = Values(s).Select(x => x ?? 0).ToArray();
        // Ключовете на атрибутите
        var keys = Grades.For(s.Group).Select(a => a.Key).ToArray();
        // Помощна функция: истина, ако атрибутът е поне B+ (9)
        bool Has(string key) => v[Array.IndexOf(keys, key)] >= 9;
        // Най-силният атрибут
        var top = keys[Array.IndexOf(v, v.Max())];
        // Ниво според OVR
        var ovr = Overall(s) ?? 0;
        var tier = ovr >= 95 ? "Generational " : ovr >= 88 ? "All-Star " : ovr >= 80 ? "" : "Developing ";
        // Тип за гардовете
        if (s.Group == "g")
        {
            // Комбинации от два силни атрибута
            var name = Has("jumpShot") && Has("handles") ? "Shot Creator"
                : Has("passing") && Has("basketballIQ") ? "Floor General"
                : Has("jumpShot") && Has("perimeterDefense") ? "3-and-D Wing"
                : Has("finishing") && (Has("bounce") || Has("speed")) ? "Slasher"
                : Has("perimeterDefense") && Has("speed") ? "Lockdown Defender"
                : Has("passing") && Has("handles") ? "Playmaker"
                // Иначе по най-силния атрибут
                : top switch
                {
                    "jumpShot" => "Sharpshooter", "finishing" => "Rim Attacker", "passing" => "Table Setter",
                    "handles" => "Ball Handler", "perimeterDefense" => "Perimeter Stopper", "speed" => "Speedster",
                    "bounce" => "High Flyer", "size" => "Big Wing", "basketballIQ" => "Coach on the Floor", _ => "Closer",
                };
            // Връщаме нивото + типа
            return tier + name;
        }
        // Тип за високите играчи
        var big = Has("jumpShot") && Has("size") ? "Stretch Big"
            : Has("playmaking") && Has("basketballIQ") ? "Point Center"
            : Has("interiorDefense") && Has("rebounding") ? "Rim Protector"
            : Has("finishing") && Has("bounce") ? "Lob Threat"
            : Has("finishing") && Has("size") ? "Post Scorer"
            : top switch
            {
                "jumpShot" => "Pick-and-Pop Big", "finishing" => "Interior Finisher", "rebounding" => "Glass Cleaner",
                "playmaking" => "Passing Big", "interiorDefense" => "Paint Anchor", "speed" => "Mobile Big",
                "bounce" => "Athletic Big", "size" => "Tower", "basketballIQ" => "Smart Big", _ => "Enforcer",
            };
        // Връщаме нивото + типа
        return tier + big;
    }

    // Отборите за барабана: текущият в средата и по два от двете страни
    public static List<string> ReelTeams(string? center)
    {
        // Всички съкращения
        var all = Teams.All.Select(t => t.Abbr).ToList();
        // Позицията на текущия (или 0)
        var i = center == null ? 0 : Math.Max(0, all.IndexOf(center));
        // Пет отбора около него (кръгово)
        return Enumerable.Range(-2, 5).Select(k => all[((i + k) % all.Count + all.Count) % all.Count]).ToList();
    }

    // Играчите за барабана: текущият в средата и по два от двете страни
    public List<BuildPlayer?> ReelPlayers(BuildState s)
    {
        // Ако няма завъртан играч, барабанът е празен
        if (s.Current == null) return new List<BuildPlayer?> { null, null, null, null, null };
        // Всички играчи от режима
        var pool = Pool(s);
        // Позицията на текущия
        var i = Math.Max(0, pool.FindIndex(p => p.Index == s.Current));
        // Пет играча около него
        return Enumerable.Range(-2, 5).Select(k => (BuildPlayer?)pool[((i + k) % pool.Count + pool.Count) % pool.Count]).ToList();
    }

    // Статистиките за сезона според оценките
    public static PlayerSeasonStats SeasonStats(BuildState s)
    {
        // Оценките (празните са 0)
        var v = Values(s).Select(x => (double)(x ?? 0)).ToArray();
        // Истина за гардовете
        var guard = s.Group == "g";
        // Малко случайност: число между -1 и 1
        double J() => (Random.Shared.NextDouble() - 0.5) * 2;
        // Без отрицателни стойности
        double R(double x) => Math.Max(0, x);
        // Кратки имена на оценките
        double js = v[0], fin = v[1], a2 = v[2], a3 = v[3], def = v[4], spd = v[5], bnc = v[6], sz = v[7], iq = v[8];
        // OVR на играча
        var ovr = Overall(s) ?? 60;
        // Връщаме всички статистики
        return new PlayerSeasonStats
        {
            // Точки: от стрелба, завършване и дрибъл/създаване
            Ppg = R(5 + 27 * (js * .35 + fin * .4 + (guard ? a3 : a2 * .3 + a3 * .7) * .25) / 11 + J() * 1.5),
            // Борби: гардовете от ръст и отскок, високите от борбата
            Rpg = guard ? R(2 + 6 * (sz * .5 + bnc * .5) / 11 + J() * .5) : R(4 + 10 * (a2 * .6 + sz * .4) / 11 + J() * .7),
            // Асистенции: от подаване и IQ
            Apg = guard ? R(1 + 9 * (a2 * .6 + a3 * .2 + iq * .2) / 11 + J() * .5) : R(1 + 7 * (a3 * .7 + iq * .3) / 11 + J() * .4),
            // Кражби: от защитата (гардове) или от бързината (високи)
            Spg = guard ? R(.4 + 1.6 * def / 11 + J() * .15) : R(.4 + .8 * spd / 11 + J() * .1),
            // Блокади: от отскок и ръст (гардове) или от защита на коша (високи)
            Bpg = guard ? R(.1 + .8 * (bnc + sz) / 22 + J() * .1) : R(.3 + 3 * (def * .6 + sz * .2 + bnc * .2) / 11 + J() * .2),
            // Процент от игра
            Fg = guard ? 40 + fin * .9 + js * .3 + J() : 45 + fin * 1.1 + J(),
            // Процент от тройки (високите със слаба стрелба не стрелят тройки)
            Tp = js < 3 && !guard ? 0 : 24 + js * 1.35 + J() * 1.5,
            // Процент от наказателни
            Ft = 58 + js * 2.5 + J() * 2,
            // Загубени топки
            Tov = Math.Max(.6, (guard ? 1 + a3 * .22 : .8 + a3 * .18) + J() * .2),
            // Рейтинг на ефективност
            Per = Math.Max(6, 7 + (ovr - 55) * .45 + J()),
        };
    }
}

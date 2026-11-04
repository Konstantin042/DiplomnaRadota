// Логиката на играта 82-0: завъртане, драфт, рейтинг на отбора и симулация на сезона
using CourtVision.Models;

namespace CourtVision.Services;

// Един показател, който стартовата петица трябва да покрие (например 32+ борби)
public class TeamNeed
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public double Min { get; set; }
    public double Penalty { get; set; }
}

// Рейтинг на отбора след драфта
public class TeamRating
{
    public double Strength { get; set; }
    public int Rating { get; set; }
    public List<TeamNeed> Needs { get; set; } = new();
    public double Points { get; set; }
    public double ShotPenalty { get; set; }
}

// Услугата за 82-0
public class ChallengeService
{
    public static readonly string[] Slots = { "PG", "SG", "SF", "PF", "C", "B0", "B1", "B2" };
    public static readonly string[] Eras = { "60", "70", "80", "90", "00", "10", "20" };

    private readonly DataService _data;

    public ChallengeService(DataService data) => _data = data;

    // Етикет на десетилетие, например "80's"
    public static string EraLabel(string decade) => decade + "'s";
    // Пълно десетилетие, например "1980s" или "2010s"
    public static string EraYears(string decade) => (string.Compare(decade, "50", StringComparison.Ordinal) >= 0 ? "19" : "20") + decade + "s";
    // Истина, ако мястото е на пейката
    public static bool IsBench(string slot) => slot.StartsWith("B");
    // Име на мястото за съобщенията
    public static string SlotName(string slot) => IsBench(slot) ? "the bench" : Positions.Info[slot].Name;

    // Играчите в даден състав
    public List<PoolPlayer> Pool(string key) => _data.Pools.TryGetValue(key, out var list) ? list : new();

    // Идентификаторите на вече избраните играчи
    private static HashSet<string> UsedIds(ChallengeState s) => s.Lineup.Values.Select(p => p.Id).ToHashSet();

    // Свободните места
    public static List<string> OpenSlots(ChallengeState s) => Slots.Where(slot => !s.Lineup.ContainsKey(slot)).ToList();

    // Има ли свободно място, на което играчът може да застане
    public static bool Fits(ChallengeState s, PoolPlayer p) => OpenSlots(s).Any(p.CanPlay);

    // Има ли в този състав поне един играч, който може да се избере
    private bool KeyHasOptions(ChallengeState s, string key)
    {
        var used = UsedIds(s);
        return Pool(key).Any(p => !used.Contains(p.Id) && Fits(s, p));
    }

    // Завъртане: mode = "all" (нов отбор и ера), "team" (само отбор) или "era" (само ера)
    public void Spin(ChallengeState s, string mode)
    {
        if (s.IsComplete) return;
        var parts = s.CurrentKey?.Split('|');
        var team = parts?[0];
        var era = parts?[1];
        List<string> options;
        if (mode == "team")
        {
            if (s.TeamRespins <= 0 || era == null) return;
            options = _data.PoolKeys.Where(k => k.EndsWith("|" + era) && k != s.CurrentKey && KeyHasOptions(s, k)).ToList();
        }
        else if (mode == "era")
        {
            if (s.EraRespins <= 0 || team == null) return;
            options = _data.PoolKeys.Where(k => k.StartsWith(team + "|") && k != s.CurrentKey && KeyHasOptions(s, k)).ToList();
        }
        else
        {
            options = _data.PoolKeys.Where(k => KeyHasOptions(s, k)).ToList();
        }
        if (options.Count == 0)
        {
            s.Message = mode == "team" ? "No other team from this era has a player for your open spots."
                      : mode == "era" ? "This team has no other era with a player for your open spots."
                      : "No players left for your open spots.";
            return;
        }
        if (mode == "team") s.TeamRespins--;
        if (mode == "era") s.EraRespins--;
        s.CurrentKey = options[Random.Shared.Next(options.Count)];
        s.Message = "";
        s.SelectedSlot = null;
    }

    // Слага избрания играч на дадено място
    public void Place(ChallengeState s, string playerId, string slot)
    {
        if (s.CurrentKey == null || s.Lineup.ContainsKey(slot)) return;
        var player = Pool(s.CurrentKey).FirstOrDefault(p => p.Id == playerId);
        if (player == null || UsedIds(s).Contains(player.Id) || !player.CanPlay(slot)) return;
        s.Lineup[slot] = player;
        s.CurrentKey = null;
        s.Round++;
        s.Games = null;
        s.Message = $"{player.Name} added to {(IsBench(slot) ? "the bench" : "the " + Positions.Info[slot].Name + " spot")}."
                  + (player.Positions.Count > 1 ? " Tap him on the court to move him later." : "");
    }

    // Натискане на място: избира играч за местене или го мести/разменя
    public void SelectSlot(ChallengeState s, string slot)
    {
        if (s.SelectedSlot == null)
        {
            if (s.Lineup.TryGetValue(slot, out var picked))
            {
                s.SelectedSlot = slot;
                s.Message = $"Moving {picked.Name} (plays {picked.PositionsText}). Tap a highlighted spot or the bench. Tap him again to cancel.";
            }
            return;
        }
        if (s.SelectedSlot == slot) { s.SelectedSlot = null; s.Message = ""; return; }
        var a = s.Lineup[s.SelectedSlot];
        s.Lineup.TryGetValue(slot, out var b);
        if (!a.CanPlay(slot)) s.Message = $"{a.Name} can't play {Positions.Info[slot].Name}. He plays {a.PositionsText}.";
        else if (b != null && !b.CanPlay(s.SelectedSlot)) s.Message = $"Can't swap: {b.Name} can't play {SlotName(s.SelectedSlot)}.";
        else
        {
            s.Lineup[slot] = a;
            if (b != null) s.Lineup[s.SelectedSlot] = b; else s.Lineup.Remove(s.SelectedSlot);
            s.Message = $"{a.Name} moved to {SlotName(slot)}.";
            s.SelectedSlot = null;
            s.Games = null;
        }
    }

    // Може ли избраният играч да отиде на това място (за подсветка)
    public static bool IsTarget(ChallengeState s, string slot)
    {
        if (s.SelectedSlot == null || slot == s.SelectedSlot) return false;
        var a = s.Lineup[s.SelectedSlot];
        s.Lineup.TryGetValue(slot, out var b);
        return a.CanPlay(slot) && (b == null || b.CanPlay(s.SelectedSlot));
    }

    // Играчите в текущия състав, подредени: първо тези, които стават, после по точки
    public List<PoolPlayer> DrawerPlayers(ChallengeState s)
    {
        if (s.CurrentKey == null) return new();
        var used = UsedIds(s);
        return Pool(s.CurrentKey)
            .Where(p => !used.Contains(p.Id))
            .OrderByDescending(p => Fits(s, p))
            .ThenByDescending(p => p.Ppg)
            .ToList();
    }

    // Изчислява рейтинга на отбора (трябват всичките 8 играча)
    public TeamRating Rate(ChallengeState s)
    {
        var starters = Positions.Order.Select(k => s.Lineup[k]).ToList();
        var bench = new[] { "B0", "B1", "B2" }.Select(k => s.Lineup[k]).ToList();
        var vs = starters.Sum(p => p.Value);
        var vb = bench.Sum(p => p.Value);
        var needs = new List<TeamNeed>
        {
            Need("Rebounds", starters.Sum(p => p.Rpg), 32, 1.5),
            Need("Assists", starters.Sum(p => p.Apg), 18, 1.5),
            Need("Blocks", starters.Sum(p => p.BlockValue), 3, 4),
            Need("Steals", starters.Sum(p => p.StealValue), 5, 4),
        };
        var points = starters.Sum(p => p.Ppg);
        // Наказание, ако петицата има над 110 точки (твърде много реализатори, една топка)
        var shotPenalty = Math.Max(0, points - 110) * 0.6;
        // Обща сила: титуляри + 35% от резервите − наказанията
        var strength = vs + 0.35 * vb - needs.Sum(n => n.Penalty) - shotPenalty;
        // Рейтинг от 1 до 99
        var rating = Math.Clamp((int)Math.Round(50 + (strength - 255) * 0.6), 1, 99);
        return new TeamRating { Strength = strength, Rating = rating, Needs = needs, Points = points, ShotPenalty = shotPenalty };
    }

    // Създава един показател с наказание, ако стойността е под минимума
    private static TeamNeed Need(string label, double value, double min, double weight) =>
        new() { Label = label, Value = value, Min = min, Penalty = Math.Max(0, min - value) * weight };

    // Симулира 82 мача и връща резултатите (true = победа)
    public List<bool> Simulate(ChallengeState s)
    {
        var strength = Rate(s).Strength;
        var games = new List<bool>();
        for (var i = 0; i < 82; i++)
        {
            // Случайна сила на противника (лош или добър ден)
            var opponent = (Random.Shared.NextDouble() * 2 - 1) * 0.6;
            // Шанс за победа (логистична функция), никога над 97.5%
            var p = Math.Min(0.975, 1 / (1 + Math.Exp(-((strength - 255) / 30 + opponent))));
            games.Add(Random.Shared.NextDouble() < p);
        }
        s.Games = games;
        return games;
    }

    // Заглавие и текст за резултата от сезона
    public static (string Title, string Text) Verdict(int wins)
    {
        var losses = 82 - wins;
        // Според броя победи избираме текст
        if (wins == 82) return ("82–0. Perfect season!", "Almost impossible. No real NBA team has ever done this; the record is 73–9.");
        if (wins >= 74) return ($"{wins}–{losses}. Better than any team in history", "You beat the 73–9 record of the 2015–16 Warriors. Perfect is still a long way off.");
        if (wins >= 60) return ($"{wins}–{losses}. Title contender", "Around 60 wins usually means one of the best teams in the league.");
        if (wins >= 45) return ($"{wins}–{losses}. Playoff team", "Good enough for the playoffs, but not a favourite. Check the red boxes next to your rating.");
        return ($"{wins}–{losses}. Missed the playoffs", "Your team lost too often. Look for more rebounds, assists and defense.");
    }
}

// Логиката на играта 82-0: завъртане, драфт, рейтинг на отбора и симулация на сезона
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Един показател, който стартовата петица трябва да покрие (например 32+ борби)
public class TeamNeed
{
    public string Label { get; set; } = ""; // Име на показателя
    public double Value { get; set; }       // Колко има отборът
    public double Min { get; set; }         // Колко е нужно
    public double Penalty { get; set; }     // Наказание, ако не стига
}

// Рейтинг на отбора след драфта
public class TeamRating
{
    public double Strength { get; set; }               // Сила на отбора (сурова стойност)
    public int Rating { get; set; }                    // Рейтинг от 1 до 99
    public List<TeamNeed> Needs { get; set; } = new(); // Показателите за баланс
    public double Points { get; set; }                 // Сбор от точките на петицата
    public double ShotPenalty { get; set; }            // Наказание за твърде много реализатори
}

// Услугата за 82-0
public class ChallengeService
{
    // Всички 8 места: 5 титуляри и 3 резерви
    public static readonly string[] Slots = { "PG", "SG", "SF", "PF", "C", "B0", "B1", "B2" };
    // Десетилетията, които може да се паднат
    public static readonly string[] Eras = { "60", "70", "80", "90", "00", "10", "20" };

    // Данните със съставите
    private readonly DataService _data;

    // Конструктор: получава данните
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
        // Взимаме вече избраните
        var used = UsedIds(s);
        // Проверяваме дали има неизбран играч, който се побира на свободно място
        return Pool(key).Any(p => !used.Contains(p.Id) && Fits(s, p));
    }

    // Завъртане: mode = "all" (нов отбор и ера), "team" (само отбор) или "era" (само ера)
    public void Spin(ChallengeState s, string mode)
    {
        // Ако драфтът е свършил, нищо не правим
        if (s.IsComplete) return;
        // Текущият отбор и ера (ако има)
        var parts = s.CurrentKey?.Split('|');
        var team = parts?[0];
        var era = parts?[1];
        // Възможните състави според вида завъртане
        List<string> options;
        if (mode == "team")
        {
            // Респин на отбора: трябва да има оставащи и вече завъртян състав
            if (s.TeamRespins <= 0 || era == null) return;
            // Същата ера, друг отбор
            options = _data.PoolKeys.Where(k => k.EndsWith("|" + era) && k != s.CurrentKey && KeyHasOptions(s, k)).ToList();
        }
        else if (mode == "era")
        {
            // Респин на ерата: трябва да има оставащи и вече завъртян състав
            if (s.EraRespins <= 0 || team == null) return;
            // Същият отбор, друга ера
            options = _data.PoolKeys.Where(k => k.StartsWith(team + "|") && k != s.CurrentKey && KeyHasOptions(s, k)).ToList();
        }
        else
        {
            // Обикновено завъртане: всеки състав с подходящ играч
            options = _data.PoolKeys.Where(k => KeyHasOptions(s, k)).ToList();
        }
        // Ако няма възможност, казваме защо
        if (options.Count == 0)
        {
            // Съобщение според вида респин
            s.Message = mode == "team" ? "No other team from this era has a player for your open spots."
                      : mode == "era" ? "This team has no other era with a player for your open spots."
                      : "No players left for your open spots.";
            return;
        }
        // Намаляваме респиновете, ако е респин
        if (mode == "team") s.TeamRespins--;
        if (mode == "era") s.EraRespins--;
        // Избираме случаен състав
        s.CurrentKey = options[Random.Shared.Next(options.Count)];
        // Изчистваме съобщението и избора за местене
        s.Message = "";
        s.SelectedSlot = null;
    }

    // Слага избрания играч на дадено място
    public void Place(ChallengeState s, string playerId, string slot)
    {
        // Трябва да има завъртян състав и мястото да е свободно
        if (s.CurrentKey == null || s.Lineup.ContainsKey(slot)) return;
        // Търсим играча в текущия състав
        var player = Pool(s.CurrentKey).FirstOrDefault(p => p.Id == playerId);
        // Ако го няма, вече е избран или не може да играе тук, спираме
        if (player == null || UsedIds(s).Contains(player.Id) || !player.CanPlay(slot)) return;
        // Слагаме го на мястото
        s.Lineup[slot] = player;
        // Изчистваме завъртяния състав (следва ново завъртане)
        s.CurrentKey = null;
        // Минаваме в следващия рунд
        s.Round++;
        // Нова симулация ще трябва при нов отбор
        s.Games = null;
        // Съобщение за играча
        s.Message = $"{player.Name} added to {(IsBench(slot) ? "the bench" : "the " + Positions.Info[slot].Name + " spot")}."
                  + (player.Positions.Count > 1 ? " Tap him on the court to move him later." : "");
    }

    // Натискане на място: избира играч за местене или го мести/разменя
    public void SelectSlot(ChallengeState s, string slot)
    {
        // Ако нищо не е избрано
        if (s.SelectedSlot == null)
        {
            // Можем да изберем само място, на което има играч
            if (s.Lineup.TryGetValue(slot, out var picked))
            {
                // Запомняме избраното място
                s.SelectedSlot = slot;
                // Казваме какво да направи
                s.Message = $"Moving {picked.Name} (plays {picked.PositionsText}). Tap a highlighted spot or the bench. Tap him again to cancel.";
            }
            return;
        }
        // Второ натискане на същото място отменя избора
        if (s.SelectedSlot == slot) { s.SelectedSlot = null; s.Message = ""; return; }
        // Играчът, когото местим
        var a = s.Lineup[s.SelectedSlot];
        // Играчът на новото място (ако има)
        s.Lineup.TryGetValue(slot, out var b);
        // Проверяваме дали може да играе на новото място
        if (!a.CanPlay(slot)) s.Message = $"{a.Name} can't play {Positions.Info[slot].Name}. He plays {a.PositionsText}.";
        // Проверяваме дали другият може да отиде на старото място
        else if (b != null && !b.CanPlay(s.SelectedSlot)) s.Message = $"Can't swap: {b.Name} can't play {SlotName(s.SelectedSlot)}.";
        else
        {
            // Слагаме първия на новото място
            s.Lineup[slot] = a;
            // Ако там е имало играч, разменяме; иначе освобождаваме старото място
            if (b != null) s.Lineup[s.SelectedSlot] = b; else s.Lineup.Remove(s.SelectedSlot);
            // Съобщение за успех
            s.Message = $"{a.Name} moved to {SlotName(slot)}.";
            // Изчистваме избора
            s.SelectedSlot = null;
            // Старата симулация вече не важи
            s.Games = null;
        }
    }

    // Може ли избраният играч да отиде на това място (за подсветка)
    public static bool IsTarget(ChallengeState s, string slot)
    {
        // Нищо не е избрано или е същото място
        if (s.SelectedSlot == null || slot == s.SelectedSlot) return false;
        // Избраният играч
        var a = s.Lineup[s.SelectedSlot];
        // Играчът на целевото място
        s.Lineup.TryGetValue(slot, out var b);
        // Истина, ако и двамата могат да играят на новите си места
        return a.CanPlay(slot) && (b == null || b.CanPlay(s.SelectedSlot));
    }

    // Играчите в текущия състав, подредени: първо тези, които стават, после по точки
    public List<PoolPlayer> DrawerPlayers(ChallengeState s)
    {
        // Ако няма завъртане, връщаме празен списък
        if (s.CurrentKey == null) return new();
        // Вече избраните играчи
        var used = UsedIds(s);
        // Неизбраните, подредени
        return Pool(s.CurrentKey)
            .Where(p => !used.Contains(p.Id))         // Махаме избраните
            .OrderByDescending(p => Fits(s, p))       // Първо тези, които имат място
            .ThenByDescending(p => p.Ppg)             // После по точки на мач
            .ToList();
    }

    // Изчислява рейтинга на отбора (трябват всичките 8 играча)
    public TeamRating Rate(ChallengeState s)
    {
        // Стартовата петица
        var starters = Positions.Order.Select(k => s.Lineup[k]).ToList();
        // Резервите
        var bench = new[] { "B0", "B1", "B2" }.Select(k => s.Lineup[k]).ToList();
        // Сбор от стойностите на титулярите
        var vs = starters.Sum(p => p.Value);
        // Сбор от стойностите на резервите
        var vb = bench.Sum(p => p.Value);
        // Нуждите на отбора: борби, асистенции, блокади, кражби
        var needs = new List<TeamNeed>
        {
            Need("Rebounds", starters.Sum(p => p.Rpg), 32, 1.5),        // Поне 32 борби
            Need("Assists", starters.Sum(p => p.Apg), 18, 1.5),         // Поне 18 асистенции
            Need("Blocks", starters.Sum(p => p.BlockValue), 3, 4),      // Поне 3 блокади
            Need("Steals", starters.Sum(p => p.StealValue), 5, 4),      // Поне 5 кражби
        };
        // Сбор от точките
        var points = starters.Sum(p => p.Ppg);
        // Наказание, ако петицата има над 110 точки (твърде много реализатори, една топка)
        var shotPenalty = Math.Max(0, points - 110) * 0.6;
        // Обща сила: титуляри + 35% от резервите − наказанията
        var strength = vs + 0.35 * vb - needs.Sum(n => n.Penalty) - shotPenalty;
        // Рейтинг от 1 до 99
        var rating = Math.Clamp((int)Math.Round(50 + (strength - 255) * 0.6), 1, 99);
        // Връщаме резултата
        return new TeamRating { Strength = strength, Rating = rating, Needs = needs, Points = points, ShotPenalty = shotPenalty };
    }

    // Създава един показател с наказание, ако стойността е под минимума
    private static TeamNeed Need(string label, double value, double min, double weight) =>
        new() { Label = label, Value = value, Min = min, Penalty = Math.Max(0, min - value) * weight };

    // Симулира 82 мача и връща резултатите (true = победа)
    public List<bool> Simulate(ChallengeState s)
    {
        // Силата на отбора
        var strength = Rate(s).Strength;
        // Списък за резултатите
        var games = new List<bool>();
        // Играем 82 мача
        for (var i = 0; i < 82; i++)
        {
            // Случайна сила на противника (лош или добър ден)
            var opponent = (Random.Shared.NextDouble() * 2 - 1) * 0.6;
            // Шанс за победа (логистична функция), никога над 97.5%
            var p = Math.Min(0.975, 1 / (1 + Math.Exp(-((strength - 255) / 30 + opponent))));
            // Хвърляме "зар" и записваме резултата
            games.Add(Random.Shared.NextDouble() < p);
        }
        // Запазваме и връщаме резултатите
        s.Games = games;
        return games;
    }

    // Заглавие и текст за резултата от сезона
    public static (string Title, string Text) Verdict(int wins)
    {
        // Броят загуби
        var losses = 82 - wins;
        // Според броя победи избираме текст
        if (wins == 82) return ("82–0. Perfect season!", "Almost impossible. No real NBA team has ever done this; the record is 73–9.");
        if (wins >= 74) return ($"{wins}–{losses}. Better than any team in history", "You beat the 73–9 record of the 2015–16 Warriors. Perfect is still a long way off.");
        if (wins >= 60) return ($"{wins}–{losses}. Title contender", "Around 60 wins usually means one of the best teams in the league.");
        if (wins >= 45) return ($"{wins}–{losses}. Playoff team", "Good enough for the playoffs, but not a favourite. Check the red boxes next to your rating.");
        return ($"{wins}–{losses}. Missed the playoffs", "Your team lost too often. Look for more rebounds, assists and defense.");
    }
}

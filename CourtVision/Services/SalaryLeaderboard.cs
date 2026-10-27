// Класацията на Salary Cap Game: пази най-добрите билдове за всеки ден в паметта на сървъра
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Една класация за всички посетители (Singleton); изчиства се при рестарт на сървъра
public class SalaryLeaderboard
{
    // Колко реда показваме
    public const int Size = 10;
    // Билдовете по ден: ключ "2026-10-27" или "inf" (безкрайната игра) → списък
    private readonly Dictionary<string, List<SalaryEntry>> _entries = new();
    // Обект за заключване: няколко посетители могат да пишат едновременно
    private readonly object _lock = new();

    // Името на посетителя за показване, например "Guest 4821" (от идентификатора на сесията)
    public static string GuestName(string owner) =>
        "Guest " + ((owner.Aggregate(17, (h, ch) => unchecked(h * 31 + ch)) & 0x7FFFFFFF) % 9000 + 1000);

    // Добавя билд в класацията на деня
    public void Add(string key, string owner, SalaryResult result, int budget)
    {
        // Новият ред
        var entry = new SalaryEntry
        {
            Owner = owner,                                     // Чий е
            Name = GuestName(owner),                           // Име
            Players = result.Players,                          // Играчите
            Overall = result.Overall,                          // Обща оценка
            Total = result.Ppg + result.Apg + result.Rpg,      // За равен резултат
            Spare = budget - result.Spent,                     // Неизхарчени пари
        };
        // Само един посетител пише наведнъж
        lock (_lock)
        {
            // Списъкът за деня (създаваме го, ако го няма)
            if (!_entries.TryGetValue(key, out var list)) _entries[key] = list = new List<SalaryEntry>();
            // Добавяме реда
            list.Add(entry);
        }
    }

    // Най-добрите билдове за деня: по обща оценка, после по PPG + APG + RPG
    public List<SalaryEntry> Top(string key)
    {
        // Само един посетител чете/пише наведнъж
        lock (_lock)
        {
            // Ако за деня няма нищо, връщаме празен списък
            if (!_entries.TryGetValue(key, out var list)) return new List<SalaryEntry>();
            // Подреждаме и взимаме първите 10
            return list.OrderByDescending(e => e.Overall).ThenByDescending(e => e.Total).Take(Size).ToList();
        }
    }
}

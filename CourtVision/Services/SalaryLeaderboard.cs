// Класацията на Salary Cap Game: пази подписаните билдове за всеки ден в таблицата SalaryScores
using CourtVision.Database;          // Базата данни (AppDbContext и SalaryScore)
using CourtVision.Models;            // Нашите модели
using Microsoft.EntityFrameworkCore; // AsNoTracking за заявките само за четене

namespace CourtVision.Services; // Пространство от имена за услугите

// Класацията (Scoped: нова за всяка заявка, заедно с връзката с базата); данните остават и след рестарт
public class SalaryLeaderboard
{
    // Колко реда показваме
    public const int Size = 10;
    // Връзката с базата данни
    private readonly AppDbContext _db;

    // Конструктор: получава връзката с базата
    public SalaryLeaderboard(AppDbContext db) => _db = db;

    // Името на гост, например "Guest 4821" (от идентификатора на сесията)
    public static string GuestName(string owner) =>
        "Guest " + ((owner.Aggregate(17, (h, ch) => unchecked(h * 31 + ch)) & 0x7FFFFFFF) % 9000 + 1000);

    // Добавя билд в класацията на деня (key е "2026-11-02" или "inf")
    public void Add(string key, HttpContext http, SalaryResult result, int budget)
    {
        // Новият ред в таблицата SalaryScores
        _db.SalaryScores.Add(new SalaryScore
        {
            DayKey = key,                                      // Денят
            Owner = http.Session.Id,                           // Чия сесия е
            UserId = ResultService.UserId(http),               // Влезлият потребител (или NULL)
            PlayerName = ResultService.PlayerName(http),       // Име за показване
            Players = string.Join(",", result.Players),        // Петимата играчи, например "12,45,301,7,88"
            Overall = result.Overall,                          // Обща оценка
            Total = result.Ppg + result.Apg + result.Rpg,      // За равен резултат
            Spare = budget - result.Spent,                     // Неизхарчени пари
            CreatedAt = DateTime.UtcNow,                       // Кога
        });
        // Записваме в базата (INSERT)
        _db.SaveChanges();
    }

    // Най-добрите билдове за деня: по обща оценка, после по PPG + APG + RPG
    public List<SalaryEntry> Top(string key)
    {
        // Заявка към таблицата SalaryScores (SELECT ... WHERE DayKey = ... ORDER BY ...)
        var rows = _db.SalaryScores.AsNoTracking()
            .Where(s => s.DayKey == key)                                // Само за този ден
            .OrderByDescending(s => s.Overall)                          // По-високата оценка първа
            .ThenByDescending(s => s.Total)                             // При равенство: повече статистики
            .ThenBy(s => s.CreatedAt)                                   // После: който е бил по-рано
            .Take(Size)                                                 // Първите 10
            .ToList();                                                  // Изпълняваме заявката
        // Превръщаме редовете в модела, който използва изгледът
        return rows.Select(s => new SalaryEntry
        {
            Owner = s.Owner,                                                    // Чия сесия е
            Name = s.PlayerName,                                                // Името
            Players = s.Players.Split(',').Select(int.Parse).ToList(),          // Номерата на играчите
            Overall = s.Overall,                                                // Обща оценка
            Total = s.Total,                                                    // Сбор от статистиките
            Spare = s.Spare,                                                    // Неизхарчени милиони
        }).ToList();
    }
}

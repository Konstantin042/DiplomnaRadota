// Класацията на Salary Cap Game: пази подписаните билдове за всеки ден в таблицата SalaryScores
using CourtVision.Database;
using CourtVision.Models;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Services;

// Класацията (Scoped: нова за всяка заявка, заедно с връзката с базата); данните остават и след рестарт
public class SalaryLeaderboard
{
    public const int Size = 10;
    private readonly AppDbContext _db;

    public SalaryLeaderboard(AppDbContext db) => _db = db;

    // Името на гост, например "Guest 4821" (от идентификатора на сесията)
    public static string GuestName(string owner) =>
        "Guest " + ((owner.Aggregate(17, (h, ch) => unchecked(h * 31 + ch)) & 0x7FFFFFFF) % 9000 + 1000);

    // Добавя билд в класацията на деня (key е "2026-11-02" или "inf")
    public void Add(string key, HttpContext http, SalaryResult result, int budget)
    {
        _db.SalaryScores.Add(new SalaryScore
        {
            DayKey = key,
            Owner = http.Session.Id,
            UserId = ResultService.UserId(http),
            PlayerName = ResultService.PlayerName(http),
            Players = string.Join(",", result.Players),
            Overall = result.Overall,
            Total = result.Ppg + result.Apg + result.Rpg,
            Spare = budget - result.Spent,
            CreatedAt = DateTime.UtcNow,
        });
        // Записваме в базата (INSERT)
        _db.SaveChanges();
    }

    // Най-добрите билдове за деня: по обща оценка, после по PPG + APG + RPG
    public List<SalaryEntry> Top(string key)
    {
        var rows = _db.SalaryScores.AsNoTracking()
            .Where(s => s.DayKey == key)
            .OrderByDescending(s => s.Overall)
            .ThenByDescending(s => s.Total)
            .ThenBy(s => s.CreatedAt)
            .Take(Size)
            .ToList();
        return rows.Select(s => new SalaryEntry
        {
            Owner = s.Owner,
            Name = s.PlayerName,
            Players = s.Players.Split(',').Select(int.Parse).ToList(),
            Overall = s.Overall,
            Total = s.Total,
            Spare = s.Spare,
        }).ToList();
    }
}

// Контролер за страницата с 30-те отбора (чете от таблиците Teams и RosterPlayers)
using CourtVision.Database;
using CourtVision.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Controllers;

// Контролерът Teams (адреси /Teams и /Teams/Details/LAL)
public class TeamsController : Controller
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db) => _db = db;

    // GET /Teams – всички отбори, подредени по конференция и дивизия
    public IActionResult Index()
    {
        var teams = _db.Teams.AsNoTracking().OrderBy(t => t.Name).ToList();
        return View(teams);
    }

    // GET /Teams/Details/NYK – информация за един отбор и най-добрите му реализатори от RosterPlayers
    public IActionResult Details(string id)
    {
        var abbr = (id ?? "").ToUpperInvariant();
        var team = _db.Teams.AsNoTracking().FirstOrDefault(t => t.Abbr == abbr);
        if (team == null) return RedirectToAction(nameof(Index));

        // Всички редове на отбора от RosterPlayers (връзката RosterPlayers.TeamAbbr → Teams.Abbr)
        var rows = _db.RosterPlayers.AsNoTracking().Where(r => r.TeamAbbr == abbr).ToList();
        // Един играч може да е в отбора в няколко десетилетия: взимаме най-добрия му сезон
        var best = rows.GroupBy(r => r.PlayerCode)
            .Select(g => g.OrderByDescending(r => r.Ppg).First())
            .OrderByDescending(r => r.Ppg)
            .ToList();
        var top = best.Take(8)
            .Select(r => new TeamScorer(r.Name, r.PlayerCode, r.Positions.Split('/')[0], r.Decade, r.Ppg, r.Rpg, r.Apg))
            .ToList();
        // Сегашният състав от CurrentPlayers, подреден по номер на фланелката
        var roster = _db.CurrentPlayers.AsNoTracking().Where(p => p.TeamAbbr == abbr).ToList()
            .OrderBy(p => p.Jersey == "" ? 999 : int.Parse(p.Jersey)).ThenBy(p => p.Jersey.Length).ToList();
        return View(new TeamPage(team, best.Count, top, roster));
    }
}

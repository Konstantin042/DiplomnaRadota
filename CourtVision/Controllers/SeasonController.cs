// Контролер за сезона след Build-a-Player
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Season (адреси /Season/...)
public class SeasonController : Controller
{
    private const string Key = "season";
    private readonly SeasonService _season;
    private readonly ResultService _results;

    public SeasonController(SeasonService season, ResultService results)
    {
        _season = season;
        _results = results;
    }

    // GET /Season – показва текущата стъпка на сезона
    public IActionResult Index()
    {
        var s = HttpContext.Session.GetObjectOrNull<SeasonState>(Key);
        if (s == null) return RedirectToAction("Index", "Build");
        // Сезонът от Salary Cap Game се показва в самата игра (/Salary/Season)
        if (s.Source == "salary") return RedirectToAction("Season", "Salary");
        return View(s);
    }

    // POST /Season/SpinTeam – случаен отбор (връща JSON за анимацията на барабана)
    [HttpPost]
    public IActionResult SpinTeam()
    {
        var team = Teams.All[Random.Shared.Next(Teams.All.Count)];
        return Json(new { abbr = team.Abbr, name = team.Name, conference = team.Conference });
    }

    // POST /Season/Start – започва сезон с избрания отбор
    [HttpPost]
    public IActionResult Start(string team)
    {
        var build = HttpContext.Session.GetObject<BuildState>(BuildController.Key);
        if (!build.Done || Teams.All.All(t => t.Abbr != team)) return RedirectToAction("Index", "Build");
        var s = _season.Start(build, team);
        HttpContext.Session.SetObject(Key, s);
        var wins = s.Wins.GetValueOrDefault(s.Team);
        // Записваме сезона в базата: резултатът е броят победи
        _results.Save(HttpContext, "build", wins, $"{s.Team} · OVR {s.Overall} · {wins}-{82 - wins} · {SeasonService.Outcome(s)}");
        return RedirectToAction(nameof(Index));
    }

    // POST /Season/Step – минава на дадена стъпка (0 отбор, 1 сезон, 2 плейофи, 3 отчет)
    [HttpPost]
    public IActionResult Step(int step)
    {
        var s = HttpContext.Session.GetObjectOrNull<SeasonState>(Key);
        if (s == null) return RedirectToAction("Index", "Build");
        s.Step = Math.Clamp(step, 0, 3);
        HttpContext.Session.SetObject(Key, s);
        if (s.Source == "salary") return RedirectToAction("Season", "Salary");
        return RedirectToAction(nameof(Index));
    }
}

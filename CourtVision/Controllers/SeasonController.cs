// Контролер за сезона след Build-a-Player
using CourtVision.Models;          // Моделите
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Season (адреси /Season/...)
public class SeasonController : Controller
{
    // Ключът на сезона в сесията
    private const string Key = "season";
    // Логиката на сезона
    private readonly SeasonService _season;

    // Конструктор: получава услугата
    public SeasonController(SeasonService season) => _season = season;

    // GET /Season – показва текущата стъпка на сезона
    public IActionResult Index()
    {
        // Четем сезона от сесията
        var s = HttpContext.Session.GetObjectOrNull<SeasonState>(Key);
        // Ако няма сезон, връщаме играча в Build-a-Player
        if (s == null) return RedirectToAction("Index", "Build");
        // Показваме страницата
        return View(s);
    }

    // POST /Season/SpinTeam – случаен отбор (връща JSON за анимацията на барабана)
    [HttpPost]
    public IActionResult SpinTeam()
    {
        // Избираме случаен отбор от всички 30
        var team = Teams.All[Random.Shared.Next(Teams.All.Count)];
        // Връщаме съкращението и името
        return Json(new { abbr = team.Abbr, name = team.Name, conference = team.Conference });
    }

    // POST /Season/Start – започва сезон с избрания отбор
    [HttpPost]
    public IActionResult Start(string team)
    {
        // Билдът от сесията
        var build = HttpContext.Session.GetObject<BuildState>(BuildController.Key);
        // Без готов билд или с непознат отбор не може да се започне
        if (!build.Done || Teams.All.All(t => t.Abbr != team)) return RedirectToAction("Index", "Build");
        // Създаваме целия сезон
        var s = _season.Start(build, team);
        // Записваме го в сесията
        HttpContext.Session.SetObject(Key, s);
        // Показваме страницата на сезона
        return RedirectToAction(nameof(Index));
    }

    // POST /Season/Step – минава на дадена стъпка (0 отбор, 1 сезон, 2 плейофи, 3 отчет)
    [HttpPost]
    public IActionResult Step(int step)
    {
        // Четем сезона
        var s = HttpContext.Session.GetObjectOrNull<SeasonState>(Key);
        // Ако няма, връщаме към билда
        if (s == null) return RedirectToAction("Index", "Build");
        // Ограничаваме стъпката между 0 и 3
        s.Step = Math.Clamp(step, 0, 3);
        // Записваме
        HttpContext.Session.SetObject(Key, s);
        // Показваме страницата
        return RedirectToAction(nameof(Index));
    }
}

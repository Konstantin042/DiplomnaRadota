// Контролер за Higher or Lower
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът HigherLower (адреси /HigherLower/...)
public class HigherLowerController : Controller
{
    private const string Key = "higherlower";
    private readonly HigherLowerService _game;
    private readonly ResultService _results;

    public HigherLowerController(HigherLowerService game, ResultService results)
    {
        _game = game;
        _results = results;
    }

    // Чете състоянието от сесията (в началото режимът е "" – менюто)
    private HigherLowerState Load() => HttpContext.Session.GetObject<HigherLowerState>(Key);

    // Записва и връща картата (за JavaScript) или пренасочва към страницата
    private IActionResult Card(HigherLowerState s)
    {
        HttpContext.Session.SetObject(Key, s);
        if (Request.IsAjax()) return PartialView(s.Mode == "" ? "_Menu" : "_Card", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /HigherLower – страницата на играта
    public IActionResult Index()
    {
        return View(Load());
    }

    // POST /HigherLower/Mode – започва игра: mode = "c" (Current) или "a" (All-Time)
    [HttpPost]
    public IActionResult Mode(string mode)
    {
        var s = Load();
        _game.Start(s, mode);
        return Card(s);
    }

    // POST /HigherLower/Menu – връщане към избора на режим
    [HttpPost]
    public IActionResult Menu()
    {
        var s = Load();
        s.Mode = "";
        return Card(s);
    }

    // POST /HigherLower/Guess – отговор: dir = "higher" или "lower"
    [HttpPost]
    public IActionResult Guess(string dir)
    {
        var s = Load();
        var wasOver = s.Over;
        _game.Guess(s, dir);
        if (!wasOver && s.Over)
            _results.Save(HttpContext, "hilo", s.Score, $"Streak of {s.Score} · {(s.Mode == "c" ? "Current" : "All-Time")}");
        return Card(s);
    }

    // POST /HigherLower/Next – следващ рунд след верен отговор
    [HttpPost]
    public IActionResult Next()
    {
        var s = Load();
        _game.Next(s);
        return Card(s);
    }

    // POST /HigherLower/Restart – нова игра
    [HttpPost]
    public IActionResult Restart()
    {
        var s = Load();
        _game.Start(s, s.Mode);
        return Card(s);
    }
}

// Контролер за Guess the Player
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Guess (адреси /Guess/...)
public class GuessController : Controller
{
    private const string Key = "guess";
    private readonly GuessService _game;
    private readonly ResultService _results;

    public GuessController(GuessService game, ResultService results)
    {
        _game = game;
        _results = results;
    }

    // Записва играта в базата, ако току-що е свършила (wasOver е състоянието преди действието)
    private void SaveIfFinished(GuessState s, bool wasOver)
    {
        if (wasOver || !s.Over) return;
        var name = _game.Player(s.Mystery).Name;
        // Победа: резултатът е броят опити; загуба: 0
        _results.Save(HttpContext, "guess", s.Won ? s.Guesses.Count : 0,
            s.Won ? $"{name} in {s.Guesses.Count} guesses" : $"{name} · not found");
    }

    // Чете състоянието; ако няма игра, започва нова
    private GuessState Load()
    {
        var s = HttpContext.Session.GetObject<GuessState>(Key);
        if (s.Mystery < 0) _game.Start(s);
        return s;
    }

    // Записва и връща таблото (за JavaScript) или пренасочва към страницата
    private IActionResult Board(GuessState s)
    {
        HttpContext.Session.SetObject(Key, s);
        if (Request.IsAjax()) return PartialView("_Board", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /Guess – страницата на играта
    public IActionResult Index()
    {
        var s = Load();
        HttpContext.Session.SetObject(Key, s);
        return View(s);
    }

    // POST /Guess/Try – опит с име на играч
    [HttpPost]
    public IActionResult Try(string name)
    {
        var s = Load();
        var wasOver = s.Over;
        _game.Guess(s, name);
        SaveIfFinished(s, wasOver);
        return Board(s);
    }

    // POST /Guess/GiveUp – показва отговора
    [HttpPost]
    public IActionResult GiveUp()
    {
        var s = Load();
        var wasOver = s.Over;
        _game.GiveUp(s);
        SaveIfFinished(s, wasOver);
        return Board(s);
    }

    // POST /Guess/More – още 4 опита за същия играч
    [HttpPost]
    public IActionResult More()
    {
        var s = Load();
        _game.More(s);
        return Board(s);
    }

    // POST /Guess/New – нов таен играч
    [HttpPost]
    public IActionResult New()
    {
        var s = Load();
        _game.Start(s);
        return Board(s);
    }
}

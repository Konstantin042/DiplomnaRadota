// Контролер за играта 82-0
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Challenge (адреси /Challenge/...)
public class ChallengeController : Controller
{
    private const string Key = "challenge";
    private readonly ChallengeService _game;
    private readonly ResultService _results;

    public ChallengeController(ChallengeService game, ResultService results)
    {
        _game = game;
        _results = results;
    }

    // Чете състоянието от сесията
    private ChallengeState Load() => HttpContext.Session.GetObject<ChallengeState>(Key);
    // Записва състоянието в сесията
    private void Save(ChallengeState s) => HttpContext.Session.SetObject(Key, s);

    // Връща само таблото (за JavaScript) или цялата страница (без JavaScript)
    private IActionResult Board(ChallengeState s)
    {
        Save(s);
        if (Request.IsAjax()) return PartialView("_Board", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /Challenge – страницата на играта
    public IActionResult Index() => View(Load());

    // POST /Challenge/Spin – завъртане (mode = all, team или era)
    [HttpPost]
    public IActionResult Spin(string mode = "all")
    {
        var s = Load();
        _game.Spin(s, mode);
        return Board(s);
    }

    // POST /Challenge/Place – слага играч на място; pick е "идентификатор|място", например "jordami01|SG"
    [HttpPost]
    public IActionResult Place(string pick)
    {
        var s = Load();
        var parts = (pick ?? "").Split('|');
        if (parts.Length == 2) _game.Place(s, parts[0], parts[1]);
        return Board(s);
    }

    // POST /Challenge/Slot – натискане на място за местене или размяна
    [HttpPost]
    public IActionResult Slot(string slot)
    {
        var s = Load();
        _game.SelectSlot(s, slot);
        return Board(s);
    }

    // POST /Challenge/Simulate – симулира 82 мача
    [HttpPost]
    public IActionResult Simulate()
    {
        var s = Load();
        if (s.IsComplete)
        {
            var games = _game.Simulate(s);
            var wins = games.Count(won => won);
            // Записваме сезона в базата: резултатът е броят победи
            _results.Save(HttpContext, "challenge", wins, $"{wins}-{82 - wins} · {ChallengeService.Verdict(wins).Title}");
        }
        return Board(s);
    }

    // POST /Challenge/Reset – нов драфт
    [HttpPost]
    public IActionResult Reset() => Board(new ChallengeState());

    // POST /Challenge/RandomPlay – бутонът Random: случаен състав от 8 играча
    [HttpPost]
    public IActionResult RandomPlay()
    {
        var s = Load();
        _game.RandomFill(s);
        return Board(s);
    }
}

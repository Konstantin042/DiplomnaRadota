// Контролер за играта 82-0
using CourtVision.Models;          // Моделите (ChallengeState)
using CourtVision.Services;        // Услугите (ChallengeService, сесия)
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Challenge (адреси /Challenge/...)
public class ChallengeController : Controller
{
    // Ключът, под който пазим играта в сесията
    private const string Key = "challenge";
    // Логиката на играта
    private readonly ChallengeService _game;

    // Конструктор: ASP.NET Core ни подава услугата автоматично
    public ChallengeController(ChallengeService game) => _game = game;

    // Чете състоянието от сесията
    private ChallengeState Load() => HttpContext.Session.GetObject<ChallengeState>(Key);
    // Записва състоянието в сесията
    private void Save(ChallengeState s) => HttpContext.Session.SetObject(Key, s);

    // Връща само таблото (за JavaScript) или цялата страница (без JavaScript)
    private IActionResult Board(ChallengeState s)
    {
        // Записваме промените
        Save(s);
        // Ако заявката е от JavaScript, връщаме само частичния изглед
        if (Request.IsAjax()) return PartialView("_Board", s);
        // Иначе пренасочваме към страницата
        return RedirectToAction(nameof(Index));
    }

    // GET /Challenge – страницата на играта
    public IActionResult Index() => View(Load());

    // POST /Challenge/Spin – завъртане (mode = all, team или era)
    [HttpPost]
    public IActionResult Spin(string mode = "all")
    {
        // Зареждаме играта
        var s = Load();
        // Завъртаме
        _game.Spin(s, mode);
        // Връщаме таблото
        return Board(s);
    }

    // POST /Challenge/Place – слага играч на място; pick е "идентификатор|място", например "jordami01|SG"
    [HttpPost]
    public IActionResult Place(string pick)
    {
        // Зареждаме играта
        var s = Load();
        // Разделяме идентификатора и мястото
        var parts = (pick ?? "").Split('|');
        // Ако има и двете части, слагаме играча
        if (parts.Length == 2) _game.Place(s, parts[0], parts[1]);
        // Връщаме таблото
        return Board(s);
    }

    // POST /Challenge/Slot – натискане на място за местене или размяна
    [HttpPost]
    public IActionResult Slot(string slot)
    {
        // Зареждаме, обработваме натискането и връщаме таблото
        var s = Load();
        _game.SelectSlot(s, slot);
        return Board(s);
    }

    // POST /Challenge/Simulate – симулира 82 мача
    [HttpPost]
    public IActionResult Simulate()
    {
        // Зареждаме играта
        var s = Load();
        // Симулираме само ако всички 8 места са попълнени
        if (s.IsComplete) _game.Simulate(s);
        // Връщаме таблото
        return Board(s);
    }

    // POST /Challenge/Reset – нов драфт
    [HttpPost]
    public IActionResult Reset() => Board(new ChallengeState());
}

// Контролер за Guess the Player
using CourtVision.Models;          // Моделите
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Guess (адреси /Guess/...)
public class GuessController : Controller
{
    // Ключът в сесията
    private const string Key = "guess";
    // Логиката на играта
    private readonly GuessService _game;

    // Конструктор: получава услугата
    public GuessController(GuessService game) => _game = game;

    // Чете състоянието; ако няма игра, започва нова
    private GuessState Load()
    {
        // Четем от сесията
        var s = HttpContext.Session.GetObject<GuessState>(Key);
        // Нова игра, ако още няма таен играч
        if (s.Mystery < 0) _game.Start(s);
        // Връщаме състоянието
        return s;
    }

    // Записва и връща таблото (за JavaScript) или пренасочва към страницата
    private IActionResult Board(GuessState s)
    {
        // Записваме
        HttpContext.Session.SetObject(Key, s);
        // За JavaScript само таблото
        if (Request.IsAjax()) return PartialView("_Board", s);
        // Иначе цялата страница
        return RedirectToAction(nameof(Index));
    }

    // GET /Guess – страницата на играта
    public IActionResult Index()
    {
        // Зареждаме (и започваме при нужда), записваме и показваме
        var s = Load();
        HttpContext.Session.SetObject(Key, s);
        return View(s);
    }

    // POST /Guess/Try – опит с име на играч
    [HttpPost]
    public IActionResult Try(string name)
    {
        // Зареждаме, проверяваме опита и връщаме таблото
        var s = Load();
        _game.Guess(s, name);
        return Board(s);
    }

    // POST /Guess/GiveUp – показва отговора
    [HttpPost]
    public IActionResult GiveUp()
    {
        // Зареждаме, отказваме се и връщаме таблото
        var s = Load();
        _game.GiveUp(s);
        return Board(s);
    }

    // POST /Guess/More – още 4 опита за същия играч
    [HttpPost]
    public IActionResult More()
    {
        // Зареждаме, добавяме опити и връщаме таблото
        var s = Load();
        _game.More(s);
        return Board(s);
    }

    // POST /Guess/New – нов таен играч
    [HttpPost]
    public IActionResult New()
    {
        // Зареждаме (за да запазим броя игри) и започваме нова
        var s = Load();
        _game.Start(s);
        return Board(s);
    }
}

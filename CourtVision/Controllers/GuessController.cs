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
    // Резултатите в базата (таблица GameResults)
    private readonly ResultService _results;

    // Конструктор: получава услугите
    public GuessController(GuessService game, ResultService results)
    {
        _game = game;       // Играта
        _results = results; // Резултатите
    }

    // Записва играта в базата, ако току-що е свършила (wasOver е състоянието преди действието)
    private void SaveIfFinished(GuessState s, bool wasOver)
    {
        // Записваме само веднъж: в момента, в който играта свършва
        if (wasOver || !s.Over) return;
        // Тайният играч
        var name = _game.Player(s.Mystery).Name;
        // Победа: резултатът е броят опити; загуба: 0
        _results.Save(HttpContext, "guess", s.Won ? s.Guesses.Count : 0,
            s.Won ? $"{name} in {s.Guesses.Count} guesses" : $"{name} · not found");
    }

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
        // Зареждаме и проверяваме опита
        var s = Load();
        var wasOver = s.Over;
        _game.Guess(s, name);
        // Ако играта свърши (познали сме), я записваме
        SaveIfFinished(s, wasOver);
        // Връщаме таблото
        return Board(s);
    }

    // POST /Guess/GiveUp – показва отговора
    [HttpPost]
    public IActionResult GiveUp()
    {
        // Зареждаме и се отказваме
        var s = Load();
        var wasOver = s.Over;
        _game.GiveUp(s);
        // Записваме загубата
        SaveIfFinished(s, wasOver);
        // Връщаме таблото
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

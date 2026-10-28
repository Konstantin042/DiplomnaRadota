// Контролер за Higher or Lower
using CourtVision.Models;          // Моделите
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът HigherLower (адреси /HigherLower/...)
public class HigherLowerController : Controller
{
    // Ключът в сесията
    private const string Key = "higherlower";
    // Логиката на играта
    private readonly HigherLowerService _game;

    // Конструктор: получава услугата
    public HigherLowerController(HigherLowerService game) => _game = game;

    // Чете състоянието от сесията (в началото режимът е "" – менюто)
    private HigherLowerState Load() => HttpContext.Session.GetObject<HigherLowerState>(Key);

    // Записва и връща картата (за JavaScript) или пренасочва към страницата
    private IActionResult Card(HigherLowerState s)
    {
        // Записваме
        HttpContext.Session.SetObject(Key, s);
        // За JavaScript само менюто или картата
        if (Request.IsAjax()) return PartialView(s.Mode == "" ? "_Menu" : "_Card", s);
        // Иначе цялата страница
        return RedirectToAction(nameof(Index));
    }

    // GET /HigherLower – страницата на играта
    public IActionResult Index()
    {
        // Зареждаме и показваме (менюто или текущата игра)
        return View(Load());
    }

    // POST /HigherLower/Mode – започва игра: mode = "c" (Current) или "a" (All-Time)
    [HttpPost]
    public IActionResult Mode(string mode)
    {
        // Зареждаме и започваме нова игра в избрания режим
        var s = Load();
        _game.Start(s, mode);
        return Card(s);
    }

    // POST /HigherLower/Menu – връщане към избора на режим
    [HttpPost]
    public IActionResult Menu()
    {
        // Режим "" означава меню (рекордите остават)
        var s = Load();
        s.Mode = "";
        return Card(s);
    }

    // POST /HigherLower/Guess – отговор: dir = "higher" или "lower"
    [HttpPost]
    public IActionResult Guess(string dir)
    {
        // Зареждаме, проверяваме отговора и връщаме картата
        var s = Load();
        _game.Guess(s, dir);
        return Card(s);
    }

    // POST /HigherLower/Next – следващ рунд след верен отговор
    [HttpPost]
    public IActionResult Next()
    {
        // Зареждаме, минаваме нататък и връщаме картата
        var s = Load();
        _game.Next(s);
        return Card(s);
    }

    // POST /HigherLower/Restart – нова игра
    [HttpPost]
    public IActionResult Restart()
    {
        // Зареждаме (за да запазим рекорда) и започваме отначало в същия режим
        var s = Load();
        _game.Start(s, s.Mode);
        return Card(s);
    }
}

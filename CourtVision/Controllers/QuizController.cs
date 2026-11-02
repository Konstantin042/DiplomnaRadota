// Контролер за Position Quiz
using CourtVision.Models;          // Моделите (QuizState)
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Quiz (адреси /Quiz/...)
public class QuizController : Controller
{
    // Ключът в сесията
    private const string Key = "quiz";
    // Логиката на куиза
    private readonly QuizService _quiz;
    // Резултатите в базата (таблица GameResults)
    private readonly ResultService _results;

    // Конструктор: получава услугите
    public QuizController(QuizService quiz, ResultService results)
    {
        _quiz = quiz;       // Куизът
        _results = results; // Резултатите
    }

    // Чете състоянието; ако куизът не е започнал, го започва
    private QuizState Load()
    {
        // Четем от сесията
        var s = HttpContext.Session.GetObject<QuizState>(Key);
        // Нов куиз, ако няма избрани играчи
        if (s.Order.Count == 0) _quiz.Start(s);
        // Връщаме състоянието
        return s;
    }

    // Записва и връща картата на куиза (или цялата страница)
    private IActionResult Card(QuizState s)
    {
        // Записваме
        HttpContext.Session.SetObject(Key, s);
        // За JavaScript само картата
        if (Request.IsAjax()) return PartialView("_Card", s);
        // Иначе цялата страница
        return RedirectToAction(nameof(Index));
    }

    // GET /Quiz – страницата на куиза
    public IActionResult Index()
    {
        // Зареждаме (и започваме при нужда) и записваме
        var s = Load();
        HttpContext.Session.SetObject(Key, s);
        // Показваме страницата
        return View(s);
    }

    // POST /Quiz/Answer – отговор с позиция
    [HttpPost]
    public IActionResult Answer(string position)
    {
        // Зареждаме, записваме отговора и връщаме картата
        var s = Load();
        _quiz.Answer(s, position);
        return Card(s);
    }

    // POST /Quiz/Next – следващ играч
    [HttpPost]
    public IActionResult Next()
    {
        // Зареждаме и минаваме нататък
        var s = Load();
        // Рундът преди това
        var before = s.Round;
        _quiz.Next(s);
        // Току-що свърши последният рунд: записваме резултата в базата
        if (before < QuizService.Rounds && s.Round == QuizService.Rounds)
            _results.Save(HttpContext, "quiz", s.Score, $"{s.Score} of {QuizService.Rounds} correct");
        // Връщаме картата
        return Card(s);
    }

    // POST /Quiz/Restart – нов куиз
    [HttpPost]
    public IActionResult Restart()
    {
        // Нов празен куиз, който веднага започва
        var s = new QuizState();
        _quiz.Start(s);
        return Card(s);
    }
}

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

    // Конструктор: получава услугата
    public QuizController(QuizService quiz) => _quiz = quiz;

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
        // Зареждаме, минаваме нататък и връщаме картата
        var s = Load();
        _quiz.Next(s);
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

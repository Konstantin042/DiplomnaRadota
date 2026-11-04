// Контролер за Position Quiz
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Quiz (адреси /Quiz/...)
public class QuizController : Controller
{
    private const string Key = "quiz";
    private readonly QuizService _quiz;
    private readonly ResultService _results;

    public QuizController(QuizService quiz, ResultService results)
    {
        _quiz = quiz;
        _results = results;
    }

    // Чете състоянието; ако куизът не е започнал, го започва
    private QuizState Load()
    {
        var s = HttpContext.Session.GetObject<QuizState>(Key);
        if (s.Order.Count == 0) _quiz.Start(s);
        return s;
    }

    // Записва и връща картата на куиза (или цялата страница)
    private IActionResult Card(QuizState s)
    {
        HttpContext.Session.SetObject(Key, s);
        if (Request.IsAjax()) return PartialView("_Card", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /Quiz – страницата на куиза
    public IActionResult Index()
    {
        var s = Load();
        HttpContext.Session.SetObject(Key, s);
        return View(s);
    }

    // POST /Quiz/Answer – отговор с позиция
    [HttpPost]
    public IActionResult Answer(string position)
    {
        var s = Load();
        _quiz.Answer(s, position);
        return Card(s);
    }

    // POST /Quiz/Next – следващ играч
    [HttpPost]
    public IActionResult Next()
    {
        var s = Load();
        var before = s.Round;
        _quiz.Next(s);
        if (before < QuizService.Rounds && s.Round == QuizService.Rounds)
            _results.Save(HttpContext, "quiz", s.Score, $"{s.Score} of {QuizService.Rounds} correct");
        return Card(s);
    }

    // POST /Quiz/Restart – нов куиз
    [HttpPost]
    public IActionResult Restart()
    {
        var s = new QuizState();
        _quiz.Start(s);
        return Card(s);
    }
}

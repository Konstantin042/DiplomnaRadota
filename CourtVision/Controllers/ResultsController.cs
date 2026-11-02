// Контролер за класациите на всички игри (от таблицата GameResults)
using CourtVision.Services;       // ResultService – резултатите от базата
using Microsoft.AspNetCore.Mvc;   // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Results (адрес /Results)
public class ResultsController : Controller
{
    // Резултатите от базата
    private readonly ResultService _results;

    // Конструктор: получава услугата
    public ResultsController(ResultService results) => _results = results;

    // GET /Results – първите 10 места във всяка игра
    public IActionResult Index()
    {
        // За всяка игра: описанието ѝ и класацията
        var boards = ResultService.Games.Select(g => new LeaderboardBox(g, _results.Top(g.Key))).ToList();
        // Показваме страницата
        return View(boards);
    }
}

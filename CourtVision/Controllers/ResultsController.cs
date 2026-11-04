// Контролер за класациите на всички игри (от таблицата GameResults)
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Results (адрес /Results)
public class ResultsController : Controller
{
    private readonly ResultService _results;

    public ResultsController(ResultService results) => _results = results;

    // GET /Results – първите 10 места във всяка игра
    public IActionResult Index()
    {
        var boards = ResultService.Games.Select(g => new LeaderboardBox(g, _results.Top(g.Key))).ToList();
        return View(boards);
    }
}

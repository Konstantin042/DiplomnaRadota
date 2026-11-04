// Контролер за Salary Cap Game (мрежа с цени и таван на заплатите)
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Salary (адреси /Salary/...)
public class SalaryController : Controller
{
    private const string Key = "salary";
    private const string SeasonKey = "season";
    private readonly SalaryService _game;
    private readonly SeasonService _season;
    private readonly SalaryLeaderboard _board;
    private readonly ResultService _results;

    public SalaryController(SalaryService game, SeasonService season, SalaryLeaderboard board, ResultService results)
    {
        _game = game;
        _season = season;
        _board = board;
        _results = results;
    }

    // Чете състоянието и се грижи текущият ден да съществува
    private SalaryState Load()
    {
        var s = HttpContext.Session.GetObject<SalaryState>(Key);
        var recent = SalaryService.RecentDates().Select(d => d.ToString("yyyy-MM-dd")).ToList();
        if (s.Current == "" || (s.Current != "inf" && !recent.Contains(s.Current))) s.Current = SalaryService.TodayKey();
        if (!s.Days.ContainsKey(s.Current)) s.Days[s.Current] = _game.NewDay(s.Current);
        return s;
    }

    // Текущият ден
    private static SalaryDay Day(SalaryState s) => s.Days[s.Current];

    // Записва и връща таблото (за JavaScript) или пренасочва към страницата
    private IActionResult Board(SalaryState s)
    {
        HttpContext.Session.SetObject(Key, s);
        if (Request.IsAjax()) return PartialView("_Board", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /Salary – страницата на играта
    public IActionResult Index()
    {
        var s = Load();
        s.Menu = ""; s.Locked = false; Day(s).Flip.Clear();
        HttpContext.Session.SetObject(Key, s);
        return View(s);
    }

    // POST /Salary/Menu – отваря или затваря меню: power (power-ups), dates (дати) или board (класация)
    [HttpPost]
    public IActionResult Menu(string menu)
    {
        var s = Load();
        var d = Day(s);
        d.Flip.Clear();
        s.Locked = false;
        if (menu == "board" && d.Key != "inf" && d.Result == null) { s.Locked = true; s.Menu = ""; return Board(s); }
        s.Menu = s.Menu == menu ? "" : menu is "power" or "dates" or "board" ? menu : "";
        return Board(s);
    }

    // POST /Salary/Mode – включва режим разбъркване (shuffle) или скаут (scout)
    [HttpPost]
    public IActionResult Mode(string mode)
    {
        var s = Load();
        SalaryService.ToggleMode(Day(s), mode);
        s.Menu = ""; s.Locked = false;
        return Board(s);
    }

    // POST /Salary/Tap – натискане на играч; cell е "колона-ред", например "2-4"
    [HttpPost]
    public IActionResult Tap(string cell)
    {
        var s = Load();
        s.Menu = ""; s.Locked = false;
        var parts = (cell ?? "").Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var col) && int.TryParse(parts[1], out var row)) _game.Tap(Day(s), col, row);
        return Board(s);
    }

    // POST /Salary/Column – разбъркване на колона (⇅)
    [HttpPost]
    public IActionResult Column(int col)
    {
        var s = Load();
        SalaryService.ShuffleColumn(Day(s), col);
        return Board(s);
    }

    // POST /Salary/Row – разбъркване на ред (⇄)
    [HttpPost]
    public IActionResult Row(int row)
    {
        var s = Load();
        SalaryService.ShuffleRow(Day(s), row);
        return Board(s);
    }

    // POST /Salary/Date – отваря друг ден или нова безкрайна игра (key = "2026-10-27" или "inf")
    [HttpPost]
    public IActionResult Date(string key)
    {
        var s = Load();
        s.Menu = ""; s.Locked = false;
        if (key == "inf") { s.Current = "inf"; s.Days["inf"] = _game.NewDay("inf"); return Board(s); }
        if (SalaryService.RecentDates().Any(d => d.ToString("yyyy-MM-dd") == key))
        {
            s.Current = key;
            if (!s.Days.ContainsKey(key)) s.Days[key] = _game.NewDay(key);
            Day(s).Flip.Clear();
        }
        return Board(s);
    }

    // POST /Salary/Lock – заключва билда, изчислява резултата и пуска сезона (обикновен формуляр, не fetch)
    [HttpPost]
    public IActionResult Lock()
    {
        var s = Load();
        var d = Day(s);
        if (d.Result != null) return ShowSeason(d);
        if (!SalaryService.AllPicked(d) || SalaryService.Spent(d) > d.Budget) return RedirectToAction(nameof(Index));
        var result = _game.Score(d);
        result.Team = SalaryService.TeamFor(d);
        d.Result = result;
        d.Mode = "";
        HttpContext.Session.SetObject(Key, s);
        _board.Add(d.Key, HttpContext, result, d.Budget);
        // Записваме изиграната игра (таблица GameResults)
        _results.Save(HttpContext, "salary", result.Overall, $"OVR {result.Overall} · ${result.Spent}M of ${d.Budget}M");
        return ShowSeason(d);
    }

    // Създава (или отваря отново) сезона за заключения билд и отива на страницата му
    private IActionResult ShowSeason(SalaryDay d)
    {
        var existing = HttpContext.Session.GetObjectOrNull<SeasonState>(SeasonKey);
        if (existing != null && existing.Source == "salary" && existing.SalaryKey == d.Key) return RedirectToAction(nameof(Season));
        var build = _game.MakeBuild(d);
        var season = _season.Start(build, d.Result!.Team);
        season.Source = "salary";
        season.SalaryKey = d.Key;
        HttpContext.Session.SetObject(SeasonKey, season);
        return RedirectToAction(nameof(Season));
    }

    // GET /Salary/Season – сезонът на подписания отбор, вътре в Salary Cap Game (същият изглед като в Build-a-Player)
    public IActionResult Season()
    {
        var s = HttpContext.Session.GetObjectOrNull<SeasonState>(SeasonKey);
        if (s == null || s.Source != "salary") return RedirectToAction(nameof(Index));
        return View("~/Views/Season/Index.cshtml", s);
    }
}

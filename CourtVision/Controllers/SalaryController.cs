// Контролер за Build-a-Player Salary (мрежа с цени и таван на заплатите)
using CourtVision.Models;          // Моделите
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Salary (адреси /Salary/...)
public class SalaryController : Controller
{
    // Ключът в сесията
    private const string Key = "salary";
    // Ключът на сезона в сесията (същият като в SeasonController)
    private const string SeasonKey = "season";
    // Логиката на играта
    private readonly SalaryService _game;
    // Симулацията на сезона
    private readonly SeasonService _season;
    // Класацията
    private readonly SalaryLeaderboard _board;

    // Конструктор: получава услугите
    public SalaryController(SalaryService game, SeasonService season, SalaryLeaderboard board)
    {
        _game = game;     // Играта
        _season = season; // Сезонът
        _board = board;   // Класацията
    }

    // Чете състоянието и се грижи текущият ден да съществува
    private SalaryState Load()
    {
        // Състоянието от сесията
        var s = HttpContext.Session.GetObject<SalaryState>(Key);
        // Позволените дати: последните 10 дни
        var recent = SalaryService.RecentDates().Select(d => d.ToString("yyyy-MM-dd")).ToList();
        // Ако няма текущ ден или е твърде стар, отваряме днешния
        if (s.Current == "" || (s.Current != "inf" && !recent.Contains(s.Current))) s.Current = SalaryService.TodayKey();
        // Ако денят още не е създаден, го създаваме
        if (!s.Days.ContainsKey(s.Current)) s.Days[s.Current] = _game.NewDay(s.Current);
        // Връщаме състоянието
        return s;
    }

    // Текущият ден
    private static SalaryDay Day(SalaryState s) => s.Days[s.Current];

    // Записва и връща таблото (за JavaScript) или пренасочва към страницата
    private IActionResult Board(SalaryState s)
    {
        // Записваме промените
        HttpContext.Session.SetObject(Key, s);
        // За JavaScript връщаме само таблото
        if (Request.IsAjax()) return PartialView("_Board", s);
        // Иначе цялата страница
        return RedirectToAction(nameof(Index));
    }

    // GET /Salary – страницата на играта
    public IActionResult Index()
    {
        // Зареждаме и записваме (за да се запомни създаденият ден)
        var s = Load();
        // Менютата и съобщенията се затварят при ново зареждане
        s.Menu = ""; s.Locked = false; Day(s).Flip.Clear();
        HttpContext.Session.SetObject(Key, s);
        // Показваме страницата
        return View(s);
    }

    // POST /Salary/Menu – отваря или затваря меню: power (power-ups), dates (дати) или board (класация)
    [HttpPost]
    public IActionResult Menu(string menu)
    {
        // Зареждаме
        var s = Load();
        var d = Day(s);
        // Изчистваме анимацията и съобщението
        d.Flip.Clear();
        s.Locked = false;
        // Класацията на деня се отключва чак след като изиграем деня
        if (menu == "board" && d.Key != "inf" && d.Result == null) { s.Locked = true; s.Menu = ""; return Board(s); }
        // Повторно натискане затваря менюто
        s.Menu = s.Menu == menu ? "" : menu is "power" or "dates" or "board" ? menu : "";
        // Връщаме таблото
        return Board(s);
    }

    // POST /Salary/Mode – включва режим разбъркване (shuffle) или скаут (scout)
    [HttpPost]
    public IActionResult Mode(string mode)
    {
        // Зареждаме, сменяме режима и затваряме менюто
        var s = Load();
        SalaryService.ToggleMode(Day(s), mode);
        s.Menu = ""; s.Locked = false;
        return Board(s);
    }

    // POST /Salary/Tap – натискане на играч; cell е "колона-ред", например "2-4"
    [HttpPost]
    public IActionResult Tap(string cell)
    {
        // Зареждаме
        var s = Load();
        s.Menu = ""; s.Locked = false;
        // Разделяме колоната и реда
        var parts = (cell ?? "").Split('-');
        // Ако са две числа, обработваме натискането
        if (parts.Length == 2 && int.TryParse(parts[0], out var col) && int.TryParse(parts[1], out var row)) _game.Tap(Day(s), col, row);
        // Връщаме таблото
        return Board(s);
    }

    // POST /Salary/Column – разбъркване на колона (⇅)
    [HttpPost]
    public IActionResult Column(int col)
    {
        // Зареждаме, разбъркваме и връщаме таблото
        var s = Load();
        SalaryService.ShuffleColumn(Day(s), col);
        return Board(s);
    }

    // POST /Salary/Row – разбъркване на ред (⇄)
    [HttpPost]
    public IActionResult Row(int row)
    {
        // Зареждаме, разбъркваме и връщаме таблото
        var s = Load();
        SalaryService.ShuffleRow(Day(s), row);
        return Board(s);
    }

    // POST /Salary/Date – отваря друг ден или нова безкрайна игра (key = "2026-10-27" или "inf")
    [HttpPost]
    public IActionResult Date(string key)
    {
        // Зареждаме
        var s = Load();
        s.Menu = ""; s.Locked = false;
        // Безкрайна игра: винаги нова мрежа
        if (key == "inf") { s.Current = "inf"; s.Days["inf"] = _game.NewDay("inf"); return Board(s); }
        // Дата: само от последните 10 дни
        if (SalaryService.RecentDates().Any(d => d.ToString("yyyy-MM-dd") == key))
        {
            // Отваряме деня (създаваме го, ако го няма)
            s.Current = key;
            if (!s.Days.ContainsKey(key)) s.Days[key] = _game.NewDay(key);
            Day(s).Flip.Clear();
        }
        // Връщаме таблото
        return Board(s);
    }

    // POST /Salary/Lock – заключва билда, изчислява резултата и пуска сезона (обикновен формуляр, не fetch)
    [HttpPost]
    public IActionResult Lock()
    {
        // Зареждаме
        var s = Load();
        var d = Day(s);
        // Ако денят вече е изигран, само показваме резултата
        if (d.Result != null) return ShowSeason(d);
        // Трябват 5 избрани играчи в рамките на тавана
        if (!SalaryService.AllPicked(d) || SalaryService.Spent(d) > d.Budget) return RedirectToAction(nameof(Index));
        // Изчисляваме резултата
        var result = _game.Score(d);
        // Отборът за сезона
        result.Team = SalaryService.TeamFor(d);
        // Заключваме деня
        d.Result = result;
        d.Mode = "";
        // Записваме, за да има сесията идентификатор
        HttpContext.Session.SetObject(Key, s);
        // Добавяме билда в класацията
        _board.Add(d.Key, HttpContext.Session.Id, result, d.Budget);
        // Пускаме сезона
        return ShowSeason(d);
    }

    // Създава (или отваря отново) сезона за заключения билд и отива на страницата му
    private IActionResult ShowSeason(SalaryDay d)
    {
        // Сезонът, който вече е в сесията
        var existing = HttpContext.Session.GetObjectOrNull<SeasonState>(SeasonKey);
        // Ако е от същия ден на Salary, просто го показваме
        if (existing != null && existing.Source == "salary" && existing.SalaryKey == d.Key) return RedirectToAction("Index", "Season");
        // Билдът от петте играчи
        var build = _game.MakeBuild(d);
        // Нов сезон с отбора от резултата
        var season = _season.Start(build, d.Result!.Team);
        // Отбелязваме, че идва от Salary
        season.Source = "salary";
        season.SalaryKey = d.Key;
        // Записваме сезона
        HttpContext.Session.SetObject(SeasonKey, season);
        // Отиваме на страницата на сезона
        return RedirectToAction("Index", "Season");
    }
}

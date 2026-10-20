// Контролер за Build-a-Player
using CourtVision.Models;          // Моделите (BuildState)
using CourtVision.Services;        // Услугите
using Microsoft.AspNetCore.Mvc;    // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Build (адреси /Build/...)
public class BuildController : Controller
{
    // Ключът в сесията
    public const string Key = "build";
    // Логиката на играта
    private readonly BuildService _game;

    // Конструктор: получава услугата
    public BuildController(BuildService game) => _game = game;

    // Чете състоянието от сесията
    private BuildState Load() => HttpContext.Session.GetObject<BuildState>(Key);
    // Записва състоянието
    private void Save(BuildState s) => HttpContext.Session.SetObject(Key, s);

    // Връща частичния изглед (менюто или играта) или пренасочва към страницата
    private IActionResult Screen(BuildState s)
    {
        // Записваме промените
        Save(s);
        // За JavaScript връщаме само съдържанието
        if (Request.IsAjax()) return PartialView(s.Mode == "" ? "_Menu" : "_Game", s);
        // Иначе цялата страница
        return RedirectToAction(nameof(Index));
    }

    // GET /Build – страницата на играта
    public IActionResult Index() => View(Load());

    // POST /Build/Group – избор Guard ("g") или Big ("b") в менюто
    [HttpPost]
    public IActionResult Group(string group)
    {
        // Зареждаме и сменяме групата (само "g" или "b")
        var s = Load();
        s.Group = group == "b" ? "b" : "g";
        return Screen(s);
    }

    // POST /Build/Start – започва билд: mode = "a" (All-Time) или "c" (Current)
    [HttpPost]
    public IActionResult Start(string mode)
    {
        // Зареждаме и започваме нов билд
        var s = Load();
        _game.Start(s, mode == "c" ? "c" : "a");
        return Screen(s);
    }

    // POST /Build/Menu – връщане към началното меню
    [HttpPost]
    public IActionResult Menu()
    {
        // Режим "" означава меню
        var s = Load();
        s.Mode = "";
        return Screen(s);
    }

    // POST /Build/Spin – завъртане (kind = all, team, player)
    [HttpPost]
    public IActionResult Spin(string kind = "all")
    {
        // Зареждаме, завъртаме и връщаме играта
        var s = Load();
        _game.Spin(s, kind);
        return Screen(s);
    }

    // POST /Build/Take – копира атрибут с даден номер
    [HttpPost]
    public IActionResult Take(int attribute)
    {
        // Зареждаме, копираме и връщаме играта
        var s = Load();
        _game.Take(s, attribute);
        return Screen(s);
    }

    // POST /Build/Tab – сменя таба в арената
    [HttpPost]
    public IActionResult Tab(string tab)
    {
        // Позволени са само трите таба
        var s = Load();
        if (tab is "skills" or "physical" or "mental") s.Tab = tab;
        return Screen(s);
    }

    // POST /Build/Reset – нов билд в същия режим
    [HttpPost]
    public IActionResult Reset()
    {
        // Започваме отначало със същия режим
        var s = Load();
        _game.Start(s, s.Mode == "" ? "a" : s.Mode);
        return Screen(s);
    }
}

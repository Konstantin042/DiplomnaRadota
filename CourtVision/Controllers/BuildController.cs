// Контролер за Build-a-Player
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Build (адреси /Build/...)
public class BuildController : Controller
{
    public const string Key = "build";
    private readonly BuildService _game;

    public BuildController(BuildService game) => _game = game;

    // Чете състоянието от сесията
    private BuildState Load() => HttpContext.Session.GetObject<BuildState>(Key);
    // Записва състоянието
    private void Save(BuildState s) => HttpContext.Session.SetObject(Key, s);

    // Връща частичния изглед (менюто или играта) или пренасочва към страницата
    private IActionResult Screen(BuildState s)
    {
        Save(s);
        if (Request.IsAjax()) return PartialView(s.Mode == "" ? "_Menu" : "_Game", s);
        return RedirectToAction(nameof(Index));
    }

    // GET /Build – страницата на играта
    public IActionResult Index() => View(Load());

    // POST /Build/Group – избор Guard ("g") или Big ("b") в менюто
    [HttpPost]
    public IActionResult Group(string group)
    {
        var s = Load();
        s.Group = group == "b" ? "b" : "g";
        return Screen(s);
    }

    // POST /Build/Start – започва билд: mode = "a" (All-Time) или "c" (Current)
    [HttpPost]
    public IActionResult Start(string mode)
    {
        var s = Load();
        _game.Start(s, mode == "c" ? "c" : "a");
        return Screen(s);
    }

    // POST /Build/Menu – връщане към началното меню
    [HttpPost]
    public IActionResult Menu()
    {
        var s = Load();
        s.Mode = "";
        return Screen(s);
    }

    // POST /Build/Spin – завъртане (kind = all, team, player)
    [HttpPost]
    public IActionResult Spin(string kind = "all")
    {
        var s = Load();
        _game.Spin(s, kind);
        return Screen(s);
    }

    // POST /Build/Take – копира атрибут с даден номер
    [HttpPost]
    public IActionResult Take(int attribute)
    {
        var s = Load();
        _game.Take(s, attribute);
        return Screen(s);
    }

    // POST /Build/Tab – сменя таба в арената
    [HttpPost]
    public IActionResult Tab(string tab)
    {
        var s = Load();
        if (tab is "skills" or "physical" or "mental") s.Tab = tab;
        return Screen(s);
    }

    // POST /Build/Reset – нов билд в същия режим
    [HttpPost]
    public IActionResult Reset()
    {
        var s = Load();
        _game.Start(s, s.Mode == "" ? "a" : s.Mode);
        return Screen(s);
    }

    // POST /Build/RandomPlay – бутонът Random: попълва всички 10 атрибута на случаен принцип
    [HttpPost]
    public IActionResult RandomPlay()
    {
        var s = Load();
        if (s.Mode == "") _game.Start(s, "a");
        _game.RandomFill(s);
        return Screen(s);
    }
}

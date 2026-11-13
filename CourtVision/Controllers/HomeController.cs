// Контролер за началната страница, NBA 101 и страницата за грешки
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Home (адреси /Home/...)
public class HomeController : Controller
{
    // GET / или /Home/Index – началната страница
    public IActionResult Index() => View();

    // GET /Home/Nba101 – урокът за начинаещи
    public IActionResult Nba101() => View();

    // GET /Home/Record – рекордният сезон 73–9 на Golden State (2015–16)
    public IActionResult Record() => View();

    // GET /Home/Error – обща страница при грешка
    public IActionResult Error() => View();
}

// Контролер за началната страница, NBA 101 и страницата за грешки
using Microsoft.AspNetCore.Mvc; // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Home (адреси /Home/...)
public class HomeController : Controller
{
    // GET / или /Home/Index – началната страница
    public IActionResult Index() => View();

    // GET /Home/Nba101 – урокът за начинаещи
    public IActionResult Nba101() => View();

    // GET /Home/Error – обща страница при грешка
    public IActionResult Error() => View();
}

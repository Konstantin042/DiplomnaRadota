// Контролер за регистрация, вход, изход и профил (ASP.NET Core Identity, таблиците AspNetUsers и др.)
using CourtVision.Models;                 // Моделите на формулярите
using CourtVision.Services;               // ResultService – резултатите от базата
using Microsoft.AspNetCore.Authorization; // [Authorize] – само за влезли потребители
using Microsoft.AspNetCore.Identity;      // UserManager и SignInManager
using Microsoft.AspNetCore.Mvc;           // Базовите класове за MVC

namespace CourtVision.Controllers; // Пространство от имена за контролерите

// Контролерът Account (адреси /Account/...)
public class AccountController : Controller
{
    // Създава и търси потребители в базата (паролите се пазят само като хеш)
    private readonly UserManager<IdentityUser> _users;
    // Вход и изход: създава и изтрива бисквитката за вход
    private readonly SignInManager<IdentityUser> _signIn;
    // Резултатите от базата (за страницата Profile)
    private readonly ResultService _results;

    // Конструктор: получава услугите
    public AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, ResultService results)
    {
        _users = users;     // Потребителите
        _signIn = signIn;   // Входът
        _results = results; // Резултатите
    }

    // GET /Account/Register – формулярът за регистрация
    [HttpGet]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterModel { ReturnUrl = returnUrl });

    // POST /Account/Register – създава нов потребител и го вписва
    [HttpPost]
    [ValidateAntiForgeryToken] // Защита: формулярът трябва да е изпратен от нашия сайт
    public async Task<IActionResult> Register(RegisterModel model)
    {
        // Ако полетата не са попълнени правилно, показваме формуляра с грешките
        if (!ModelState.IsValid) return View(model);
        // Новият потребител (без имейл)
        var user = new IdentityUser { UserName = model.UserName };
        // Записваме го в таблицата AspNetUsers; паролата се хешира автоматично
        var result = await _users.CreateAsync(user, model.Password);
        // Успех: вписваме го веднага и го връщаме обратно
        if (result.Succeeded)
        {
            // Създава бисквитката за вход
            await _signIn.SignInAsync(user, isPersistent: false);
            // Връщаме го там, откъдето е дошъл
            return BackTo(model.ReturnUrl);
        }
        // Грешки от Identity (например заето име): показваме ги над формуляра
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        // Показваме формуляра отново
        return View(model);
    }

    // GET /Account/Login – формулярът за вход
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginModel { ReturnUrl = returnUrl });

    // POST /Account/Login – проверява името и паролата
    [HttpPost]
    [ValidateAntiForgeryToken] // Защита: формулярът трябва да е изпратен от нашия сайт
    public async Task<IActionResult> Login(LoginModel model)
    {
        // Ако полетата са празни, показваме формуляра с грешките
        if (!ModelState.IsValid) return View(model);
        // Проверяваме паролата спрямо хеша в базата и при успех създаваме бисквитката
        var result = await _signIn.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
        // Успех: връщаме го обратно
        if (result.Succeeded) return BackTo(model.ReturnUrl);
        // Неуспех: обща грешка (не казваме дали името или паролата е грешна)
        ModelState.AddModelError(string.Empty, "Wrong username or password.");
        // Показваме формуляра отново
        return View(model);
    }

    // POST /Account/Logout – изход
    [HttpPost]
    [ValidateAntiForgeryToken] // Защита: формулярът трябва да е изпратен от нашия сайт
    public async Task<IActionResult> Logout()
    {
        // Изтриваме бисквитката за вход
        await _signIn.SignOutAsync();
        // Отиваме на началната страница
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Profile – резултатите на влезлия потребител
    [Authorize] // Само за влезли потребители (иначе пренасочва към /Account/Login)
    public IActionResult Profile()
    {
        // Идентификаторът на потребителя (колоната Id в AspNetUsers)
        var userId = _users.GetUserId(User) ?? "";
        // Потребителското име
        var name = User.Identity?.Name ?? "";
        // Събираме данните за страницата
        var model = new ProfileModel
        {
            UserName = name,                    // Името
            Bests = _results.Bests(userId),     // Най-добрите резултати
            Recent = _results.Recent(userId),   // Последните игри
        };
        // Мястото в класацията за всяка игра, която е играл
        foreach (var key in model.Bests.Keys) model.Ranks[key] = _results.Rank(key, name);
        // Показваме страницата
        return View(model);
    }

    // Връща потребителя на адрес от нашия сайт или на началната страница (защита от пренасочване към чужд сайт)
    private IActionResult BackTo(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
}

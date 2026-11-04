// Контролер за регистрация, вход, изход и профил (ASP.NET Core Identity, таблиците AspNetUsers и др.)
using CourtVision.Models;
using CourtVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

// Контролерът Account (адреси /Account/...)
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _users;
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly ResultService _results;

    public AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, ResultService results)
    {
        _users = users;
        _signIn = signIn;
        _results = results;
    }

    // GET /Account/Register – формулярът за регистрация
    [HttpGet]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterModel { ReturnUrl = returnUrl });

    // POST /Account/Register – създава нов потребител и го вписва
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new IdentityUser { UserName = model.UserName };
        // Записваме го в таблицата AspNetUsers; паролата се хешира автоматично
        var result = await _users.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Създава бисквитката за вход
            await _signIn.SignInAsync(user, isPersistent: false);
            return BackTo(model.ReturnUrl);
        }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    // GET /Account/Login – формулярът за вход
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginModel { ReturnUrl = returnUrl });

    // POST /Account/Login – проверява името и паролата
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        // Проверяваме паролата спрямо хеша в базата и при успех създаваме бисквитката
        var result = await _signIn.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded) return BackTo(model.ReturnUrl);
        ModelState.AddModelError(string.Empty, "Wrong username or password.");
        return View(model);
    }

    // POST /Account/Logout – изход
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Profile – резултатите на влезлия потребител
    [Authorize]
    public IActionResult Profile()
    {
        var userId = _users.GetUserId(User) ?? "";
        var name = User.Identity?.Name ?? "";
        var model = new ProfileModel
        {
            UserName = name,
            Bests = _results.Bests(userId),
            Recent = _results.Recent(userId),
        };
        foreach (var key in model.Bests.Keys) model.Ranks[key] = _results.Rank(key, name);
        return View(model);
    }

    // Връща потребителя на адрес от нашия сайт или на началната страница (защита от пренасочване към чужд сайт)
    private IActionResult BackTo(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
}

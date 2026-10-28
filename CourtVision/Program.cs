// Входна точка на приложението: тук се настройва и стартира уеб сървърът
using System.Globalization;   // Настройки за език и формат на числата
using CourtVision.Services; // Достъп до нашите услуги (данни, игри, снимки)

// Числата да се пишат с точка (19.8), а не със запетая, дори на български Windows
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
// Същото и за текстовете на интерфейса
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Създаваме "строител" на уеб приложението с настройките по подразбиране
var builder = WebApplication.CreateBuilder(args);

// Добавяме поддръжка за MVC: контролери и Razor изгледи
builder.Services.AddControllersWithViews();
// Паметта, в която сесиите пазят данните си, е в RAM на сървъра
builder.Services.AddDistributedMemoryCache();
// Включваме сесии, за да помним състоянието на всяка игра за всеки посетител
builder.Services.AddSession(options =>
{
    // Сесията изтича след 2 часа без активност
    options.IdleTimeout = TimeSpan.FromHours(2);
    // Бисквитката на сесията е недостъпна за JavaScript (по-сигурно)
    options.Cookie.HttpOnly = true;
    // Бисквитката е нужна, за да работи играта, затова е "задължителна"
    options.Cookie.IsEssential = true;
});

// Услугата с данните се зарежда веднъж и се споделя от всички (Singleton)
builder.Services.AddSingleton<DataService>();
// Услугата за снимките също е една за цялото приложение
builder.Services.AddSingleton<PhotoService>();
// Логиката на играта 82-0 (не пази състояние, затова може да е Singleton)
builder.Services.AddSingleton<ChallengeService>();
// Логиката на Build-a-Player
builder.Services.AddSingleton<BuildService>();
// Симулацията на сезон, Play-In, плейофи и финали
builder.Services.AddSingleton<SeasonService>();
// Логиката на Salary Cap Game (мрежа с цени и таван на заплатите)
builder.Services.AddSingleton<SalaryService>();
// Класацията на Salary Cap Game (обща за всички посетители, в паметта на сървъра)
builder.Services.AddSingleton<SalaryLeaderboard>();
// Логиката на Higher or Lower
builder.Services.AddSingleton<HigherLowerService>();
// Логиката на Position Quiz
builder.Services.AddSingleton<QuizService>();

// Създаваме самото приложение от събраните настройки
var app = builder.Build();

// Ако не сме в режим за разработка, показваме обща страница за грешка
if (!app.Environment.IsDevelopment())
{
    // При грешка пренасочваме към /Home/Error
    app.UseExceptionHandler("/Home/Error");
}

// Позволяваме на сървъра да връща файлове от wwwroot (CSS, JS, снимки)
app.UseStaticFiles();
// Включваме маршрутизацията (кой адрес към кой контролер отива)
app.UseRouting();
// Включваме сесиите за всяка заявка
app.UseSession();

// Маршрут по подразбиране: /Контролер/Действие/ид, началото е Home/Index
app.MapControllerRoute(
    name: "default", // Име на маршрута
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Шаблон на адреса

// Стартираме сървъра и чакаме заявки
app.Run();

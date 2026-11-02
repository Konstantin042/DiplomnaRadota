// Входна точка на приложението: тук се настройва и стартира уеб сървърът
using System.Globalization;   // Настройки за език и формат на числата
using CourtVision.Database;   // Базата данни (AppDbContext и попълването ѝ)
using CourtVision.Services; // Достъп до нашите услуги (данни, игри, снимки)
using Microsoft.AspNetCore.Identity;  // Потребители, пароли и вход (ASP.NET Core Identity)
using Microsoft.EntityFrameworkCore;  // UseSqlServer – връзката с SQL Server

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

// Връзката с базата данни: SQL Server LocalDB, адресът е в appsettings.json ("DefaultConnection")
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Регистрация и вход: потребителите и паролите се пазят в таблиците AspNetUsers и др.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Паролата трябва да е поне 6 знака
    options.Password.RequiredLength = 6;
    // Не изискваме цифра, главна буква и специален знак (сайтът е за игра, не за банка)
    options.Password.RequireDigit = false;
    // Не изискваме главна буква
    options.Password.RequireUppercase = false;
    // Не изискваме малка буква
    options.Password.RequireLowercase = false;
    // Не изискваме специален знак
    options.Password.RequireNonAlphanumeric = false;
    // Не изискваме имейл: регистрацията е само с потребителско име
    options.User.RequireUniqueEmail = false;
})
    .AddEntityFrameworkStores<AppDbContext>() // Потребителите се пазят в нашата база
    .AddDefaultTokenProviders();              // Стандартните кодове (например за смяна на парола)
// Бисквитката за вход: къде да пратим посетител, който не е влязъл
builder.Services.ConfigureApplicationCookie(options =>
{
    // Страницата за вход
    options.LoginPath = "/Account/Login";
    // Страницата при липса на права
    options.AccessDeniedPath = "/Account/Login";
    // Входът се помни 14 дни (ако е избрано "Remember me")
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});
// Записването и четенето на резултатите и класациите (нова връзка с базата за всяка заявка)
builder.Services.AddScoped<ResultService>();

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
// Класацията на Salary Cap Game (в таблицата SalaryScores, нова връзка с базата за всяка заявка)
builder.Services.AddScoped<SalaryLeaderboard>();
// Логиката на Higher or Lower
builder.Services.AddSingleton<HigherLowerService>();
// Логиката на Guess the Player
builder.Services.AddSingleton<GuessService>();
// Логиката на Position Quiz
builder.Services.AddSingleton<QuizService>();

// Създаваме самото приложение от събраните настройки
var app = builder.Build();

// Създаваме базата (ако я няма) и я попълваме с отборите и играчите от папка Data
using (var scope = app.Services.CreateScope())
{
    // Връзката с базата
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Създава базата CourtVision и всички таблици, ако още не съществуват
    db.Database.EnsureCreated();
    // Попълва празните таблици от JSON файловете (само първия път)
    DbSeeder.Seed(db, Path.Combine(app.Environment.ContentRootPath, "Data"));
}

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
// Разпознава влезлия потребител от бисквитката за вход
app.UseAuthentication();
// Проверява правата (например страницата Profile е само за влезли потребители)
app.UseAuthorization();
// Включваме сесиите за всяка заявка
app.UseSession();

// Маршрут по подразбиране: /Контролер/Действие/ид, началото е Home/Index
app.MapControllerRoute(
    name: "default", // Име на маршрута
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Шаблон на адреса

// Стартираме сървъра и чакаме заявки
app.Run();

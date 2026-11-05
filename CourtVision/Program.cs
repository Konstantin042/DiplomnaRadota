// Входна точка на приложението: тук се настройва и стартира уеб сървърът
using System.Globalization;
using CourtVision.Database;
using CourtVision.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Числата да се пишат с точка (19.8), а не със запетая, дори на български Windows
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
// Включваме сесии, за да помним състоянието на всяка игра за всеки посетител
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Връзката с базата данни: SQLite файл courtvision.db в папката на проекта (името е в appsettings.json)
var dbFile = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Database:File"] ?? "courtvision.db");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbFile}"));
// Регистрация и вход: потребителите и паролите се пазят в таблиците AspNetUsers и др.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});
builder.Services.AddScoped<ResultService>();

// Данните и логиката на игрите: една инстанция за целия сайт
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<PhotoService>();
builder.Services.AddSingleton<ChallengeService>();
builder.Services.AddSingleton<BuildService>();
builder.Services.AddSingleton<SeasonService>();
builder.Services.AddSingleton<SalaryService>();
builder.Services.AddScoped<SalaryLeaderboard>();
builder.Services.AddSingleton<HigherLowerService>();
builder.Services.AddSingleton<GuessService>();
builder.Services.AddSingleton<QuizService>();

var app = builder.Build();

// Създаваме базата (ако я няма) и я попълваме с отборите и играчите от папка Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Създава базата CourtVision и всички таблици, ако още не съществуват
    db.Database.EnsureCreated();
    // Попълва празните таблици от JSON файловете (само първия път)
    DbSeeder.Seed(db, Path.Combine(app.Environment.ContentRootPath, "Data"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
// Разпознава влезлия потребител от бисквитката за вход
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Маршрут по подразбиране: /Контролер/Действие/ид, началото е Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

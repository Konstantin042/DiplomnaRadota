// Връзката с базата данни: всички таблици и връзките между тях (Entity Framework Core)
using Microsoft.AspNetCore.Identity;                         // IdentityUser – потребителите
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;     // IdentityDbContext – таблиците за вход и регистрация
using Microsoft.EntityFrameworkCore;                         // DbContext, DbSet и настройките на модела

namespace CourtVision.Database; // Пространство от имена за базата данни

// Контекстът на базата: наследява IdentityDbContext, който добавя таблиците AspNetUsers, AspNetRoles и др.
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // Конструктор: получава настройките (кой сървър и коя база) от Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Таблица Teams – 30-те отбора
    public DbSet<Team> Teams => Set<Team>();
    // Таблица RosterPlayers – играчите от съставите по отбор и десетилетие
    public DbSet<RosterPlayer> RosterPlayers => Set<RosterPlayer>();
    // Таблица RatedPlayers – играчите с оценки по 10 атрибута
    public DbSet<RatedPlayer> RatedPlayers => Set<RatedPlayer>();
    // Таблица PlayerGrades – оценките на играчите (10 реда за всеки играч)
    public DbSet<PlayerGrade> PlayerGrades => Set<PlayerGrade>();
    // Таблица QuizEntries – играчите за Position Quiz
    public DbSet<QuizEntry> QuizEntries => Set<QuizEntry>();
    // Таблица GameResults – изиграните игри
    public DbSet<GameResult> GameResults => Set<GameResult>();
    // Таблица SalaryScores – класацията на Salary Cap Game
    public DbSet<SalaryScore> SalaryScores => Set<SalaryScore>();

    // Настройва връзките, индексите и поведението при изтриване
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Първо Identity създава своите таблици (AspNetUsers и др.)
        base.OnModelCreating(builder);

        // RosterPlayers.TeamAbbr → Teams.Abbr (един отбор има много играчи в съставите)
        builder.Entity<RosterPlayer>()
            .HasOne(r => r.Team)                  // Всеки ред има един отбор
            .WithMany(t => t.RosterPlayers)       // Отборът има много редове
            .HasForeignKey(r => r.TeamAbbr);      // Чрез колоната TeamAbbr
        // Индекс за бързо търсене на състав по отбор и десетилетие
        builder.Entity<RosterPlayer>().HasIndex(r => new { r.TeamAbbr, r.Decade });

        // RatedPlayers.TeamAbbr → Teams.Abbr
        builder.Entity<RatedPlayer>()
            .HasOne(p => p.Team)                  // Всеки играч има един отбор
            .WithMany(t => t.RatedPlayers)        // Отборът има много оценени играчи
            .HasForeignKey(p => p.TeamAbbr);      // Чрез колоната TeamAbbr

        // PlayerGrades.RatedPlayerId → RatedPlayers.Id (при изтриване на играча се трият и оценките му)
        builder.Entity<PlayerGrade>()
            .HasOne(g => g.RatedPlayer)           // Всяка оценка е на един играч
            .WithMany(p => p.Grades)              // Играчът има 10 оценки
            .HasForeignKey(g => g.RatedPlayerId)  // Чрез колоната RatedPlayerId
            .OnDelete(DeleteBehavior.Cascade);    // Изтриване заедно с играча
        // Един играч не може да има две оценки за един и същ атрибут
        builder.Entity<PlayerGrade>().HasIndex(g => new { g.RatedPlayerId, g.Slot }).IsUnique();

        // QuizEntries.TeamAbbr → Teams.Abbr (без списък от страната на отбора)
        builder.Entity<QuizEntry>()
            .HasOne(q => q.Team)                  // Всеки играч от куиза има един отбор
            .WithMany()                           // Отборът няма нужда от списък с тях
            .HasForeignKey(q => q.TeamAbbr);      // Чрез колоната TeamAbbr

        // GameResults.UserId → AspNetUsers.Id (ако потребителят се изтрие, резултатите остават като на гост)
        builder.Entity<GameResult>()
            .HasOne(r => r.User)                  // Резултатът е на един потребител (или на никого)
            .WithMany()                           // Потребителят има много резултати
            .HasForeignKey(r => r.UserId)         // Чрез колоната UserId
            .OnDelete(DeleteBehavior.SetNull);    // При изтриване UserId става NULL
        // Индекс за класациите: търсим по игра и подреждаме по резултат
        builder.Entity<GameResult>().HasIndex(r => new { r.Game, r.Score });

        // SalaryScores.UserId → AspNetUsers.Id
        builder.Entity<SalaryScore>()
            .HasOne(s => s.User)                  // Билдът е на един потребител (или на гост)
            .WithMany()                           // Потребителят има много билдове
            .HasForeignKey(s => s.UserId)         // Чрез колоната UserId
            .OnDelete(DeleteBehavior.SetNull);    // При изтриване UserId става NULL
        // Индекс за класацията на деня
        builder.Entity<SalaryScore>().HasIndex(s => s.DayKey);
    }
}

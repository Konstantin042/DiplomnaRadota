// Връзката с базата данни: всички таблици и връзките между тях (Entity Framework Core)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Database;

// Контекстът на базата: наследява IdentityDbContext, който добавя таблиците AspNetUsers, AspNetRoles и др.
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // Конструктор: получава настройките (кой сървър и коя база) от Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<RosterPlayer> RosterPlayers => Set<RosterPlayer>();
    public DbSet<RatedPlayer> RatedPlayers => Set<RatedPlayer>();
    public DbSet<PlayerGrade> PlayerGrades => Set<PlayerGrade>();
    public DbSet<QuizEntry> QuizEntries => Set<QuizEntry>();
    public DbSet<GameResult> GameResults => Set<GameResult>();
    public DbSet<SalaryScore> SalaryScores => Set<SalaryScore>();

    // Настройва връзките, индексите и поведението при изтриване
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // RosterPlayers.TeamAbbr → Teams.Abbr (един отбор има много играчи в съставите)
        builder.Entity<RosterPlayer>()
            .HasOne(r => r.Team)
            .WithMany(t => t.RosterPlayers)
            .HasForeignKey(r => r.TeamAbbr);
        // Индекс за бързо търсене на състав по отбор и десетилетие
        builder.Entity<RosterPlayer>().HasIndex(r => new { r.TeamAbbr, r.Decade });

        // RatedPlayers.TeamAbbr → Teams.Abbr
        builder.Entity<RatedPlayer>()
            .HasOne(p => p.Team)
            .WithMany(t => t.RatedPlayers)
            .HasForeignKey(p => p.TeamAbbr);

        // PlayerGrades.RatedPlayerId → RatedPlayers.Id (при изтриване на играча се трият и оценките му)
        builder.Entity<PlayerGrade>()
            .HasOne(g => g.RatedPlayer)
            .WithMany(p => p.Grades)
            .HasForeignKey(g => g.RatedPlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        // Един играч не може да има две оценки за един и същ атрибут
        builder.Entity<PlayerGrade>().HasIndex(g => new { g.RatedPlayerId, g.Slot }).IsUnique();

        // QuizEntries.TeamAbbr → Teams.Abbr (без списък от страната на отбора)
        builder.Entity<QuizEntry>()
            .HasOne(q => q.Team)
            .WithMany()
            .HasForeignKey(q => q.TeamAbbr);

        // GameResults.UserId → AspNetUsers.Id (ако потребителят се изтрие, резултатите остават като на гост)
        builder.Entity<GameResult>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        // Индекс за класациите: търсим по игра и подреждаме по резултат
        builder.Entity<GameResult>().HasIndex(r => new { r.Game, r.Score });

        // SalaryScores.UserId → AspNetUsers.Id
        builder.Entity<SalaryScore>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        // Индекс за класацията на деня
        builder.Entity<SalaryScore>().HasIndex(s => s.DayKey);
    }
}

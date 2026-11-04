// Таблица GameResults: всяка изиграна игра (резултат, кой и кога я е изиграл)
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CourtVision.Database;

// Един ред от таблицата GameResults
public class GameResult
{
    public int Id { get; set; }
    // Външен ключ към AspNetUsers (NULL, ако е играл гост без вход)
    [MaxLength(450)]
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }
    // Името за класацията: потребителското име или "Guest 1234"
    [Required, MaxLength(60)]
    public string PlayerName { get; set; } = "";
    // Коя игра: "challenge", "build", "hilo", "salary", "quiz" или "guess"
    [Required, MaxLength(20)]
    public string Game { get; set; } = "";
    public int Score { get; set; }
    // Кратко описание, например "LAL · 58-24 · NBA Champions!"
    [MaxLength(200)]
    public string Details { get; set; } = "";
    public DateTime PlayedAt { get; set; }
}

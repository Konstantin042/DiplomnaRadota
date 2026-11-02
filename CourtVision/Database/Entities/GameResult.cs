// Таблица GameResults: всяка изиграна игра (резултат, кой и кога я е изиграл)
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните
using Microsoft.AspNetCore.Identity;         // IdentityUser – потребителят от таблицата AspNetUsers

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата GameResults
public class GameResult
{
    // Първичен ключ (номерира се автоматично от базата)
    public int Id { get; set; }
    // Външен ключ към AspNetUsers (NULL, ако е играл гост без вход)
    [MaxLength(450)]
    public string? UserId { get; set; }
    // Навигация към потребителя
    public IdentityUser? User { get; set; }
    // Името за класацията: потребителското име или "Guest 1234"
    [Required, MaxLength(60)]
    public string PlayerName { get; set; } = "";
    // Коя игра: "challenge", "build", "hilo", "salary", "quiz" или "guess"
    [Required, MaxLength(20)]
    public string Game { get; set; } = "";
    // Резултатът (победи, серия, OVR, верни отговори или брой опити)
    public int Score { get; set; }
    // Кратко описание, например "LAL · 58-24 · NBA Champions!"
    [MaxLength(200)]
    public string Details { get; set; } = "";
    // Кога е изиграна (по UTC)
    public DateTime PlayedAt { get; set; }
}

// Таблица InjuredPlayers: контузените играчи от NBA (обновява се на всеки 10 минути от ESPN)
using System.ComponentModel.DataAnnotations;

namespace CourtVision.Database;

public class InjuredPlayer
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string PlayerName { get; set; } = "";
    // Името без диакритика и с малки букви (Jokić → nikola jokic), по него се търси
    [Required, MaxLength(60)]
    public string NormName { get; set; } = "";
    [MaxLength(40)]
    public string TeamName { get; set; } = "";
    // "Out", "Day-To-Day"...
    [MaxLength(30)]
    public string Status { get; set; } = "";
    // Кратко описание от доклада, например "Knee"
    [MaxLength(200)]
    public string Detail { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

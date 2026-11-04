// Таблица RosterPlayers: играч от състав (един отбор в едно десетилетие) с реални статистики
using System.ComponentModel.DataAnnotations;

namespace CourtVision.Database;

// Един ред от таблицата RosterPlayers (използва се в 82-0, Higher or Lower и Guess the Player)
public class RosterPlayer
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    // Идентификатор на играча от Basketball-Reference, например "jamesle01"
    [Required, MaxLength(12)]
    public string PlayerCode { get; set; } = "";
    // Името на играча
    [Required, MaxLength(60)]
    public string Name { get; set; } = "";
    // Външен ключ към таблицата Teams
    [Required, MaxLength(3)]
    public string TeamAbbr { get; set; } = "";
    public Team? Team { get; set; }
    // Десетилетието, например "80" за 1980-те
    [Required, MaxLength(2)]
    public string Decade { get; set; } = "";
    // Позициите, разделени с "/", например "SF/SG" (първата е основната)
    [Required, MaxLength(20)]
    public string Positions { get; set; } = "";
    public double Ppg { get; set; }
    public double Rpg { get; set; }
    public double Apg { get; set; }
    public double? Spg { get; set; }
    public double? Bpg { get; set; }
}

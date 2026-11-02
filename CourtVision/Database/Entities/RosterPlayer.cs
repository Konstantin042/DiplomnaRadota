// Таблица RosterPlayers: играч от състав (един отбор в едно десетилетие) с реални статистики
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата RosterPlayers (използва се в 82-0, Higher or Lower и Guess the Player)
public class RosterPlayer
{
    // Първичен ключ (номерира се автоматично от базата)
    public int Id { get; set; }
    // Пореден номер, за да четем редовете в същия ред, в който са записани
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
    // Навигация към отбора
    public Team? Team { get; set; }
    // Десетилетието, например "80" за 1980-те
    [Required, MaxLength(2)]
    public string Decade { get; set; } = "";
    // Позициите, разделени с "/", например "SF/SG" (първата е основната)
    [Required, MaxLength(20)]
    public string Positions { get; set; } = "";
    // Точки на мач
    public double Ppg { get; set; }
    // Борби на мач
    public double Rpg { get; set; }
    // Асистенции на мач
    public double Apg { get; set; }
    // Откраднати топки на мач (NULL, ако не са се броели преди 1973–74)
    public double? Spg { get; set; }
    // Блокади на мач (NULL, ако не са се броели)
    public double? Bpg { get; set; }
}

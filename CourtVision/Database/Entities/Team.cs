// Таблица Teams: 30-те отбора в NBA
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата Teams
public class Team
{
    // Първичен ключ: съкращението на отбора, например "LAL"
    [Key, MaxLength(3)]
    public string Abbr { get; set; } = "";
    // Пълното име, например "Los Angeles Lakers"
    [Required, MaxLength(40)]
    public string Name { get; set; } = "";
    // Основният цвят на отбора, например "#552583"
    [Required, MaxLength(7)]
    public string Color { get; set; } = "";
    // Конференцията: "East" или "West"
    [Required, MaxLength(4)]
    public string Conference { get; set; } = "";

    // Връзка едно-към-много: играчите от съставите на този отбор
    public List<RosterPlayer> RosterPlayers { get; set; } = new();
    // Връзка едно-към-много: оценените играчи на този отбор
    public List<RatedPlayer> RatedPlayers { get; set; } = new();
}

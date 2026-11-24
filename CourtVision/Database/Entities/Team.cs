// Таблица Teams: 30-те отбора в NBA
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtVision.Database;

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
    // Дивизията, например "Pacific"
    [MaxLength(12)]
    public string Division { get; set; } = "";
    // Градът и залата, в която играе отборът
    [MaxLength(40)]
    public string City { get; set; } = "";
    [MaxLength(40)]
    public string Arena { get; set; } = "";
    // Годината, от която отборът е в NBA
    public int JoinedYear { get; set; }
    // Годините на титлите, разделени със запетая, например "1970,1973,2026"
    [MaxLength(200)]
    public string Titles { get; set; } = "";
    // Трима известни играчи, разделени с "|"
    [MaxLength(120)]
    public string Legends { get; set; } = "";
    // Кратка история на отбора
    [MaxLength(400)]
    public string History { get; set; } = "";

    [NotMapped]
    public List<int> TitleYears => Titles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
    [NotMapped]
    public string[] LegendNames => Legends.Split('|', StringSplitOptions.RemoveEmptyEntries);

    public List<RosterPlayer> RosterPlayers { get; set; } = new();
    public List<RatedPlayer> RatedPlayers { get; set; } = new();
    public List<CurrentPlayer> CurrentPlayers { get; set; } = new();
}

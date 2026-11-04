// Таблица RatedPlayers: играч с оценки по 10 атрибута (Build-a-Player, Salary Cap Game, сезона)
using System.ComponentModel.DataAnnotations;

namespace CourtVision.Database;

// Един ред от таблицата RatedPlayers
public class RatedPlayer
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    // Режим: "a" = All-Time (легенди), "c" = Current (днешни играчи)
    [Required, MaxLength(1)]
    public string Mode { get; set; } = "";
    // Група: "g" = Guard (гард), "b" = Big (висок играч)
    [Required, MaxLength(1)]
    public string Group { get; set; } = "";
    // Името на играча
    [Required, MaxLength(60)]
    public string Name { get; set; } = "";
    // Външен ключ към таблицата Teams
    [Required, MaxLength(3)]
    public string TeamAbbr { get; set; } = "";
    public Team? Team { get; set; }
    // Годините в отбора, например "1984–98" (празно за днешните играчи)
    [MaxLength(20)]
    public string Years { get; set; } = "";
    public int Height { get; set; }
    public int Weight { get; set; }
    // Идентификатор от Basketball-Reference (празен за някои новобранци)
    [MaxLength(12)]
    public string PlayerCode { get; set; } = "";
    public bool Rookie { get; set; }
    public double? Ppg { get; set; }
    public double? Rpg { get; set; }
    public double? Apg { get; set; }
    public double? Spg { get; set; }
    public double? Bpg { get; set; }

    public List<PlayerGrade> Grades { get; set; } = new();
}

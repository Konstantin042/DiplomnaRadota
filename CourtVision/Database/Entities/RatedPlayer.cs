// Таблица RatedPlayers: играч с оценки по 10 атрибута (Build-a-Player, Salary Cap Game, сезона)
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата RatedPlayers
public class RatedPlayer
{
    // Първичен ключ (номерира се автоматично от базата)
    public int Id { get; set; }
    // Пореден номер: играчите се пазят в сесията по този номер, затова редът не трябва да се променя
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
    // Навигация към отбора
    public Team? Team { get; set; }
    // Годините в отбора, например "1984–98" (празно за днешните играчи)
    [MaxLength(20)]
    public string Years { get; set; } = "";
    // Ръст в инчове
    public int Height { get; set; }
    // Тегло в паундове
    public int Weight { get; set; }
    // Идентификатор от Basketball-Reference (празен за някои новобранци)
    [MaxLength(12)]
    public string PlayerCode { get; set; } = "";
    // Истина, ако е новобранец без мачове в NBA
    public bool Rookie { get; set; }
    // Реални статистики (NULL, ако играчът още няма мачове)
    public double? Ppg { get; set; }
    // Борби на мач
    public double? Rpg { get; set; }
    // Асистенции на мач
    public double? Apg { get; set; }
    // Откраднати топки на мач
    public double? Spg { get; set; }
    // Блокади на мач
    public double? Bpg { get; set; }

    // Връзка едно-към-много: 10-те оценки на играча (таблица PlayerGrades)
    public List<PlayerGrade> Grades { get; set; } = new();
}

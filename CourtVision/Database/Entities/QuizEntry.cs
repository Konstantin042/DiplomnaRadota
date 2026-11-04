// Таблица QuizEntries: играч за Position Quiz с 5 умения и статистики
using System.ComponentModel.DataAnnotations;

namespace CourtVision.Database;

// Един ред от таблицата QuizEntries
public class QuizEntry
{
    public int Id { get; set; }
    // Името на играча
    [Required, MaxLength(60)]
    public string Name { get; set; } = "";
    // Позициите, разделени с "/" – това е верният отговор в куиза
    [Required, MaxLength(20)]
    public string Positions { get; set; } = "";
    // Външен ключ към таблицата Teams
    [Required, MaxLength(3)]
    public string TeamAbbr { get; set; } = "";
    public Team? Team { get; set; }
    // Десетилетието на най-добрите му години, например "90s"
    [Required, MaxLength(4)]
    public string Decade { get; set; } = "";
    public int Overall { get; set; }
    public int JumpShot { get; set; }
    public int Handles { get; set; }
    public int Finishing { get; set; }
    public int Defense { get; set; }
    public int Size { get; set; }
    public double Ppg { get; set; }
    public double Rpg { get; set; }
    public double Apg { get; set; }
    public double Spg { get; set; }
    public double Bpg { get; set; }
}

// Таблица QuizEntries: играч за Position Quiz с 5 умения и статистики
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата QuizEntries
public class QuizEntry
{
    // Първичен ключ (номерира се автоматично от базата)
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
    // Навигация към отбора
    public Team? Team { get; set; }
    // Десетилетието на най-добрите му години, например "90s"
    [Required, MaxLength(4)]
    public string Decade { get; set; } = "";
    // Обща оценка от 1 до 99
    public int Overall { get; set; }
    // Умение: стрелба от разстояние (0–99)
    public int JumpShot { get; set; }
    // Умение: дрибъл (0–99)
    public int Handles { get; set; }
    // Умение: завършване до коша (0–99)
    public int Finishing { get; set; }
    // Умение: защита (0–99)
    public int Defense { get; set; }
    // Умение: ръст и сила (0–99)
    public int Size { get; set; }
    // Точки на мач
    public double Ppg { get; set; }
    // Борби на мач
    public double Rpg { get; set; }
    // Асистенции на мач
    public double Apg { get; set; }
    // Откраднати топки на мач
    public double Spg { get; set; }
    // Блокади на мач
    public double Bpg { get; set; }
}

// Таблица PlayerGrades: една оценка (от F до S) на един играч за един атрибут
namespace CourtVision.Database;

// Един ред от таблицата PlayerGrades (всеки играч има 10 такива реда)
public class PlayerGrade
{
    public int Id { get; set; }
    public int RatedPlayerId { get; set; }
    public RatedPlayer? RatedPlayer { get; set; }
    public int Slot { get; set; }
    public int Value { get; set; }
}

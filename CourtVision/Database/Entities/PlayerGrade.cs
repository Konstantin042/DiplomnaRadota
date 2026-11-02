// Таблица PlayerGrades: една оценка (от F до S) на един играч за един атрибут
namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата PlayerGrades (всеки играч има 10 такива реда)
public class PlayerGrade
{
    // Първичен ключ (номерира се автоматично от базата)
    public int Id { get; set; }
    // Външен ключ към таблицата RatedPlayers
    public int RatedPlayerId { get; set; }
    // Навигация към играча
    public RatedPlayer? RatedPlayer { get; set; }
    // Номерът на атрибута от 0 до 9 (например 0 = Jump shot, 1 = Finishing)
    public int Slot { get; set; }
    // Оценката от 0 (F) до 11 (S)
    public int Value { get; set; }
}

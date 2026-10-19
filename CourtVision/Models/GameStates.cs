// Състоянията на игрите, които се пазят в сесията на всеки посетител
namespace CourtVision.Models; // Пространство от имена за моделите

// Състояние на играта 82-0 (драфт от 8 рунда)
public class ChallengeState
{
    // Кой играч на кое място е: "PG", "SG", "SF", "PF", "C" и резерви "B0", "B1", "B2"
    public Dictionary<string, PoolPlayer> Lineup { get; set; } = new();
    // Текущият рунд (от 1 до 8; 9 означава, че драфтът е завършен)
    public int Round { get; set; } = 1;
    // Завъртяният състав, например "BOS|60" (null, ако още няма завъртане)
    public string? CurrentKey { get; set; }
    // Оставащи респинове за отбор
    public int TeamRespins { get; set; } = 2;
    // Оставащи респинове за ера (десетилетие)
    public int EraRespins { get; set; } = 2;
    // Мястото на игрището, което играчът е избрал за преместване
    public string? SelectedSlot { get; set; }
    // Съобщение към играча след последното действие
    public string Message { get; set; } = "";
    // Резултатите от последната симулация (true = победа)
    public List<bool>? Games { get; set; }
    // Истина, ако всички 8 места са попълнени
    public bool IsComplete => Round > 8;
}


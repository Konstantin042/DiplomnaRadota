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

// Атрибут, копиран от реален играч
public class TakenAttribute
{
    // Номерът на играча в списъка с играчи
    public int PlayerIndex { get; set; }
    // Оценката (от 0 = F до 11 = S)
    public int Value { get; set; }
}

// Състояние на Build-a-Player
public class BuildState
{
    // Режим: "" = началното меню, "a" = All-Time, "c" = Current
    public string Mode { get; set; } = "";
    // Група: "g" = Guard, "b" = Big
    public string Group { get; set; } = "g";
    // Отвореният таб в арената: skills, physical или mental
    public string Tab { get; set; } = "skills";
    // Завъртяният отбор
    public string? Team { get; set; }
    // Номерът на завъртяния играч (null, ако няма)
    public int? Current { get; set; }
    // Оставащ респин за отбор
    public int TeamRespins { get; set; } = 1;
    // Оставащ респин за играч
    public int PlayerRespins { get; set; } = 1;
    // Попълнените атрибути: номер на атрибута → копирана оценка
    public Dictionary<int, TakenAttribute> Taken { get; set; } = new();
    // Номерата на вече използваните играчи (да не се падат пак)
    public HashSet<int> Used { get; set; } = new();
    // Истина, когато са попълнени всичките 10 атрибута
    public bool Done => Taken.Count == 10;
}


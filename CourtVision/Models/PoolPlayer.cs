// Играч в играта 82-0: реални статистики за един отбор в едно десетилетие
namespace CourtVision.Models; // Пространство от имена за моделите

// Един играч от състав (отбор + десетилетие)
public class PoolPlayer
{
    // Името на играча
    public string Name { get; set; } = "";
    // Идентификатор от Basketball-Reference (уникален за всеки играч)
    public string Id { get; set; } = "";
    // Позициите, на които може да играе (първата е основната)
    public List<string> Positions { get; set; } = new();
    // Отборът (съкращение)
    public string Team { get; set; } = "";
    // Десетилетието, например "80"
    public string Decade { get; set; } = "";
    // Точки на мач
    public double Ppg { get; set; }
    // Борби на мач
    public double Rpg { get; set; }
    // Асистенции на мач
    public double Apg { get; set; }
    // Откраднати топки на мач (null, ако не са се броели преди 1973–74)
    public double? Spg { get; set; }
    // Блокирани удари на мач (null, ако не са се броели)
    public double? Bpg { get; set; }

    // Основната позиция на играча
    public string MainPosition => Positions.Count > 0 ? Positions[0] : "PG";
    // Откраднати топки: истинската стойност или приблизителна оценка за старите играчи
    public double StealValue => Spg ?? Math.Round(0.5 + 0.1 * Apg + 0.015 * Ppg, 1);
    // Блокади: истинската стойност или приблизителна оценка
    public double BlockValue => Bpg ?? Math.Round(0.1 + 0.06 * Rpg, 1);
    // Истина, ако кражбите и блокадите са приблизителни
    public bool IsEstimated => Spg == null || Bpg == null;
    // Обща стойност на играча, сбор от статистиките с тегла
    public double Value => Ppg + 1.2 * Rpg + 1.6 * Apg + 3 * StealValue + 3 * BlockValue;
    // Позициите като текст, например "SF • SG"
    public string PositionsText => string.Join(" • ", Positions);
    // Може ли играчът да играе на дадено място (резервите приемат всеки)
    public bool CanPlay(string slot) => slot.StartsWith("B") || Positions.Contains(slot);
}

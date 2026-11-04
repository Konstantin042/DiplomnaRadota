// Играч в играта 82-0: реални статистики за един отбор в едно десетилетие
namespace CourtVision.Models;

// Един играч от състав (отбор + десетилетие)
public class PoolPlayer
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public List<string> Positions { get; set; } = new();
    public string Team { get; set; } = "";
    public string Decade { get; set; } = "";
    public double Ppg { get; set; }
    public double Rpg { get; set; }
    public double Apg { get; set; }
    public double? Spg { get; set; }
    public double? Bpg { get; set; }

    public string MainPosition => Positions.Count > 0 ? Positions[0] : "PG";
    // Откраднати топки: истинската стойност или приблизителна оценка за старите играчи
    public double StealValue => Spg ?? Math.Round(0.5 + 0.1 * Apg + 0.015 * Ppg, 1);
    // Блокади: истинската стойност или приблизителна оценка
    public double BlockValue => Bpg ?? Math.Round(0.1 + 0.06 * Rpg, 1);
    public bool IsEstimated => Spg == null || Bpg == null;
    // Обща стойност на играча, сбор от статистиките с тегла
    public double Value => Ppg + 1.2 * Rpg + 1.6 * Apg + 3 * StealValue + 3 * BlockValue;
    public string PositionsText => string.Join(" • ", Positions);
    // Може ли играчът да играе на дадено място (резервите приемат всеки)
    public bool CanPlay(string slot) => slot.StartsWith("B") || Positions.Contains(slot);
}

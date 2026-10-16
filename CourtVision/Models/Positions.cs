// Петте позиции в баскетбола и обясненията за тях
namespace CourtVision.Models; // Пространство от имена за моделите

// Описание на една позиция
public record PositionInfo(string Code, string Name, string Height, string Description);

// Статичен клас с всички позиции
public static class Positions
{
    // Позициите в реда, в който се показват (от гард до център)
    public static readonly string[] Order = { "PG", "SG", "SF", "PF", "C" };

    // Пълната информация за всяка позиция
    public static readonly IReadOnlyDictionary<string, PositionInfo> Info = new Dictionary<string, PositionInfo>
    {
        // Разпределител: организира играта
        ["PG"] = new("PG", "Point Guard", "1.85–1.95 m", "The organiser. Brings the ball up the court and starts the plays, like a playmaker in football. Best at dribbling and passing."),
        // Гард-стрелец: отбелязва точки
        ["SG"] = new("SG", "Shooting Guard", "1.93–2.01 m", "The scorer. Moves without the ball to get open and shoots, often from behind the 3-point line."),
        // Леко крило: прави от всичко по малко
        ["SF"] = new("SF", "Small Forward", "2.01–2.08 m", "The all-rounder. Tall enough to fight near the basket and quick enough to guard fast players."),
        // Тежко крило: играе силово до коша
        ["PF"] = new("PF", "Power Forward", "2.06–2.11 m", "The muscle. Scores close to the basket, grabs rebounds and guards big opponents."),
        // Център: най-високият играч
        ["C"] = new("C", "Center", "2.11 m +", "The tallest player. Protects the rim, blocks shots and catches missed shots (rebounds)."),
    };

    // Координати на всяка позиция върху игрището (в проценти: ляво, горе)
    public static readonly IReadOnlyDictionary<string, (int Left, int Top)> Spot = new Dictionary<string, (int, int)>
    {
        ["C"] = (35, 16),  // Центърът е до коша, вляво
        ["PF"] = (65, 16), // Тежкото крило е до коша, вдясно
        ["SF"] = (13, 62), // Лекото крило е на левия фланг
        ["SG"] = (87, 62), // Гард-стрелецът е на десния фланг
        ["PG"] = (50, 72), // Разпределителят е отгоре по средата
    };
}

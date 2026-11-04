// Оценките (F до S) и атрибутите в Build-a-Player
namespace CourtVision.Models;

// Един атрибут: ключ, етикет и кратък етикет
public record AttributeInfo(string Key, string Label, string Short);

// Статичен клас с оценките и атрибутите
public static class Grades
{
    public static readonly string[] Letters = { "F", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "S" };

    // Буква за дадено число (числото се ограничава между 0 и 11)
    public static string Letter(double value) => Letters[Math.Clamp((int)Math.Round(value), 0, 11)];

    // Цвят на оценката: S лилаво, A синьо, B зелено, C жълто, D оранжево, F червено
    public static string Color(int value) =>
        value >= 11 ? "#A78BFA" : value >= 9 ? "#3B82F6" : value >= 6 ? "#22C55E" : value >= 3 ? "#EAB308" : value >= 1 ? "#F97316" : "#EF4444";

    public static readonly AttributeInfo[] Guard =
    {
        new("jumpShot", "Jump shot", "SHOT"),
        new("finishing", "Finishing", "FINISH"),
        new("passing", "Passing", "PASS"),
        new("handles", "Handles", "HANDLES"),
        new("perimeterDefense", "Perimeter defense", "DEFENSE"),
        new("speed", "Speed", "SPEED"),
        new("bounce", "Bounce", "BOUNCE"),
        new("size", "Size", "SIZE"),
        new("basketballIQ", "Basketball IQ", "IQ"),
        new("clutch", "Leadership / clutch", "CLUTCH"),
    };

    public static readonly AttributeInfo[] Big =
    {
        new("jumpShot", "Jump shot", "SHOT"),
        new("finishing", "Finishing", "FINISH"),
        new("rebounding", "Rebounding", "REBOUND"),
        new("playmaking", "Playmaking", "PLAYMAKE"),
        new("interiorDefense", "Interior defense", "RIM D"),
        new("speed", "Speed", "SPEED"),
        new("bounce", "Bounce", "BOUNCE"),
        new("size", "Size", "SIZE"),
        new("basketballIQ", "Basketball IQ", "IQ"),
        new("clutch", "Leadership / clutch", "CLUTCH"),
    };

    // Връща атрибутите за дадена група ("g" или "b")
    public static AttributeInfo[] For(string group) => group == "b" ? Big : Guard;

    // Към кой таб принадлежи атрибутът с този номер
    public static string TabOf(int index) => index < 5 ? "skills" : index < 8 ? "physical" : "mental";

    // Трите таба: ключ и име
    public static readonly (string Key, string Label)[] Tabs = { ("skills", "Skills"), ("physical", "Physical"), ("mental", "Mental") };
}

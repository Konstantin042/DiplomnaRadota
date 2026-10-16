// Оценките (F до S) и атрибутите в Build-a-Player
namespace CourtVision.Models; // Пространство от имена за моделите

// Един атрибут: ключ, етикет и кратък етикет
public record AttributeInfo(string Key, string Label, string Short);

// Статичен клас с оценките и атрибутите
public static class Grades
{
    // Буквените оценки от 0 до 11
    public static readonly string[] Letters = { "F", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "S" };

    // Буква за дадено число (числото се ограничава между 0 и 11)
    public static string Letter(double value) => Letters[Math.Clamp((int)Math.Round(value), 0, 11)];

    // Цвят на оценката: S лилаво, A синьо, B зелено, C жълто, D оранжево, F червено
    public static string Color(int value) =>
        value >= 11 ? "#A78BFA" : value >= 9 ? "#3B82F6" : value >= 6 ? "#22C55E" : value >= 3 ? "#EAB308" : value >= 1 ? "#F97316" : "#EF4444";

    // Атрибутите на гардовете (5 умения, 3 физически, 2 ментални)
    public static readonly AttributeInfo[] Guard =
    {
        new("jumpShot", "Jump shot", "SHOT"),                 // Стрелба от разстояние
        new("finishing", "Finishing", "FINISH"),              // Завършване до коша
        new("passing", "Passing", "PASS"),                    // Подаване
        new("handles", "Handles", "HANDLES"),                 // Дрибъл
        new("perimeterDefense", "Perimeter defense", "DEFENSE"), // Защита далеч от коша
        new("speed", "Speed", "SPEED"),                       // Бързина
        new("bounce", "Bounce", "BOUNCE"),                    // Отскок
        new("size", "Size", "SIZE"),                          // Ръст и сила
        new("basketballIQ", "Basketball IQ", "IQ"),           // Разбиране на играта
        new("clutch", "Leadership / clutch", "CLUTCH"),       // Лидерство в важните моменти
    };

    // Атрибутите на високите играчи (други умения, същите физически и ментални)
    public static readonly AttributeInfo[] Big =
    {
        new("jumpShot", "Jump shot", "SHOT"),                 // Стрелба от разстояние
        new("finishing", "Finishing", "FINISH"),              // Завършване до коша
        new("rebounding", "Rebounding", "REBOUND"),           // Борба под коша
        new("playmaking", "Playmaking", "PLAYMAKE"),          // Създаване на игра
        new("interiorDefense", "Interior defense", "RIM D"),  // Защита на коша
        new("speed", "Speed", "SPEED"),                       // Бързина
        new("bounce", "Bounce", "BOUNCE"),                    // Отскок
        new("size", "Size", "SIZE"),                          // Ръст и сила
        new("basketballIQ", "Basketball IQ", "IQ"),           // Разбиране на играта
        new("clutch", "Leadership / clutch", "CLUTCH"),       // Лидерство
    };

    // Връща атрибутите за дадена група ("g" или "b")
    public static AttributeInfo[] For(string group) => group == "b" ? Big : Guard;

    // Към кой таб принадлежи атрибутът с този номер
    public static string TabOf(int index) => index < 5 ? "skills" : index < 8 ? "physical" : "mental";

    // Трите таба: ключ и име
    public static readonly (string Key, string Label)[] Tabs = { ("skills", "Skills"), ("physical", "Physical"), ("mental", "Mental") };
}

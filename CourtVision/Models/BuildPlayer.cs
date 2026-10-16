// Играч в Build-a-Player с 10 оценки (от F до S)
namespace CourtVision.Models; // Пространство от имена за моделите

// Един играч, от когото може да се копира атрибут
public class BuildPlayer
{
    // Пореден номер в списъка (използва се като ключ)
    public int Index { get; set; }
    // Режим: "a" = All-Time (легенди), "c" = Current (днешни играчи)
    public string Mode { get; set; } = "";
    // Група: "g" = Guard (гард), "b" = Big (висок играч)
    public string Group { get; set; } = "";
    // Име на играча
    public string Name { get; set; } = "";
    // Отбор (съкращение)
    public string Team { get; set; } = "";
    // Години в отбора, например "1984–98" (празно за днешните играчи)
    public string Years { get; set; } = "";
    // Ръст в инчове
    public int Height { get; set; }
    // Тегло в паундове
    public int Weight { get; set; }
    // Идентификатор от Basketball-Reference (за снимката)
    public string Id { get; set; } = "";
    // Истина, ако е новобранец без мачове в NBA
    public bool Rookie { get; set; }
    // 10 оценки от 0 (F) до 11 (S) в реда на атрибутите
    public int[] Grades { get; set; } = new int[10];
    // Реални статистики: точки, борби, асистенции, кражби, блокади
    public double[] RealStats { get; set; } = Array.Empty<double>();

    // Средна оценка от всичките 10 атрибута
    public double AverageGrade => Grades.Average();
    // Ръстът като текст във футове и инчове, например 6'8"
    public string HeightText => Height > 0 ? $"{Height / 12}'{Height % 12}\"" : "--";
}

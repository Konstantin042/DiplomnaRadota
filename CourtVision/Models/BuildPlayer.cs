// Играч в Build-a-Player с 10 оценки (от F до S)
namespace CourtVision.Models;

// Един играч, от когото може да се копира атрибут
public class BuildPlayer
{
    public int Index { get; set; }
    public string Mode { get; set; } = "";
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public string Team { get; set; } = "";
    public string Years { get; set; } = "";
    public int Height { get; set; }
    public int Weight { get; set; }
    public string Id { get; set; } = "";
    public bool Rookie { get; set; }
    public int[] Grades { get; set; } = new int[10];
    public double[] RealStats { get; set; } = Array.Empty<double>();

    public double AverageGrade => Grades.Average();
    public string HeightText => Height > 0 ? $"{Height / 12}'{Height % 12}\"" : "--";
}

// Играч за Position Quiz и за секцията NBA 101
namespace CourtVision.Models;

// Един играч с позиции и 5 умения от 0 до 99
public class QuizPlayer
{
    public string Name { get; set; } = "";
    public List<string> Positions { get; set; } = new();
    public string Team { get; set; } = "";
    public string Decade { get; set; } = "";
    public int Overall { get; set; }
    public int[] Skills { get; set; } = new int[5];
    public double[] Stats { get; set; } = new double[5];

    public string MainPosition => Positions.Count > 0 ? Positions[0] : "PG";
}

// Етикетите на 5-те умения в куиза
public static class QuizSkills
{
    public static readonly string[] Labels = { "Jump shot", "Handles", "Finishing", "Defense", "Size" };
}

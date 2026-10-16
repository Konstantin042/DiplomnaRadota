// Играч за Position Quiz и за секцията NBA 101
namespace CourtVision.Models; // Пространство от имена за моделите

// Един играч с позиции и 5 умения от 0 до 99
public class QuizPlayer
{
    // Име на играча
    public string Name { get; set; } = "";
    // Позициите (първата е основната)
    public List<string> Positions { get; set; } = new();
    // Отборът, с който е най-известен
    public string Team { get; set; } = "";
    // Десетилетието на най-добрите му години, например "90s"
    public string Decade { get; set; } = "";
    // Обща оценка от 1 до 99
    public int Overall { get; set; }
    // 5 умения: стрелба, дрибъл, завършване, защита, ръст
    public int[] Skills { get; set; } = new int[5];
    // Статистики: точки, борби, асистенции, кражби, блокади
    public double[] Stats { get; set; } = new double[5];

    // Основната позиция
    public string MainPosition => Positions.Count > 0 ? Positions[0] : "PG";
}

// Етикетите на 5-те умения в куиза
public static class QuizSkills
{
    // Имена на уменията в същия ред като масива Skills
    public static readonly string[] Labels = { "Jump shot", "Handles", "Finishing", "Defense", "Size" };
}

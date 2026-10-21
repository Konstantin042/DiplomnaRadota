// Логиката на Position Quiz: 8 рунда, познай позицията по уменията
using CourtVision.Models; // Нашите модели

namespace CourtVision.Services; // Пространство от имена за услугите

// Услугата за куиза
public class QuizService
{
    // Брой рундове в един куиз
    public const int Rounds = 8;
    // Всички данни
    private readonly DataService _data;

    // Конструктор: получава данните
    public QuizService(DataService data) => _data = data;

    // Всички играчи за куиза
    public IReadOnlyList<QuizPlayer> Players => _data.QuizPlayers;

    // Започва нов куиз с 8 случайни играча
    public void Start(QuizState s)
    {
        // Разбъркваме номерата на всички играчи и взимаме първите 8
        s.Order = Enumerable.Range(0, Players.Count).OrderBy(_ => Random.Shared.Next()).Take(Rounds).ToList();
        // Нулираме рунда, точките и отговора
        s.Round = 0; s.Score = 0; s.Answer = null;
    }

    // Играчът в текущия рунд (null, ако куизът е свършил или не е започнал)
    public QuizPlayer? Current(QuizState s) =>
        s.Order.Count == Rounds && s.Round < Rounds ? Players[s.Order[s.Round]] : null;

    // Записва отговора: позицията, която е избрал играчът
    public void Answer(QuizState s, string position)
    {
        // Текущият играч
        var player = Current(s);
        // Ако няма играч, вече има отговор или позицията е невалидна, спираме
        if (player == null || s.Answer != null || !Positions.Order.Contains(position)) return;
        // Запомняме отговора
        s.Answer = position;
        // Ако е вярно (играчът играе на тази позиция), добавяме точка
        if (player.Positions.Contains(position)) s.Score++;
    }

    // Минава на следващия рунд
    public void Next(QuizState s)
    {
        // Може да продължим само след отговор
        if (s.Answer == null) return;
        // Следващ рунд
        s.Round++;
        // Изчистваме отговора
        s.Answer = null;
    }

    // Най-силното умение на играча (за обяснението)
    public static (string Label, int Value) BestSkill(QuizPlayer p)
    {
        // Позицията на най-голямото число
        var i = Array.IndexOf(p.Skills, p.Skills.Max());
        // Етикет и стойност
        return (QuizSkills.Labels[i], p.Skills[i]);
    }

    // Заглавие за крайния резултат
    public static string FinalTitle(int score) =>
        score >= 7 ? "You could be a scout" : score >= 5 ? "You know the positions" : score >= 3 ? "Getting there" : "Keep learning";

    // Цвят на лентата за умение (по-високо = по-добре), като оценка от 0 до 11
    public static int SkillGrade(int value) =>
        value >= 97 ? 11 : value >= 93 ? 10 : value >= 90 ? 9 : value >= 87 ? 8 : value >= 83 ? 7 : value >= 80 ? 6 :
        value >= 77 ? 5 : value >= 73 ? 4 : value >= 70 ? 3 : value >= 65 ? 2 : value >= 60 ? 1 : 0;
}

// Състоянията на игрите, които се пазят в сесията на всеки посетител
namespace CourtVision.Models;

// Състояние на играта 82-0 (драфт от 8 рунда)
public class ChallengeState
{
    public Dictionary<string, PoolPlayer> Lineup { get; set; } = new();
    public int Round { get; set; } = 1;
    public string? CurrentKey { get; set; }
    public int TeamRespins { get; set; } = 2;
    public int EraRespins { get; set; } = 2;
    public string? SelectedSlot { get; set; }
    public string Message { get; set; } = "";
    public List<bool>? Games { get; set; }
    public bool IsComplete => Round > 8;
}

// Атрибут, копиран от реален играч
public class TakenAttribute
{
    public int PlayerIndex { get; set; }
    public int Value { get; set; }
}

// Състояние на Build-a-Player
public class BuildState
{
    // Дали да се пропускат контузените играчи (от настройката "Hide injured players"; задава се при всяка заявка)
    public bool HideInjured { get; set; }
    public string Mode { get; set; } = "";
    public string Group { get; set; } = "g";
    public string Tab { get; set; } = "skills";
    public string? Team { get; set; }
    public int? Current { get; set; }
    public int TeamRespins { get; set; } = 1;
    public int PlayerRespins { get; set; } = 1;
    public Dictionary<int, TakenAttribute> Taken { get; set; } = new();
    public HashSet<int> Used { get; set; } = new();
    public bool Done => Taken.Count == 10;
}

// Един мач от редовния сезон
public class SeasonGame
{
    public string Opponent { get; set; } = "";
    public bool Win { get; set; }
}

// Серия между два отбора (до 4 победи, или 1 мач в Play-In)
public class Series
{
    public string A { get; set; } = "";
    public string B { get; set; } = "";
    public List<bool> Games { get; set; } = new();
    public List<int> ScoresA { get; set; } = new();
    public List<int> ScoresB { get; set; } = new();
    public int WinsA => Games.Count(g => g);
    public int WinsB => Games.Count(g => !g);
    public string Winner => WinsA > WinsB ? A : B;
    public string Loser => WinsA > WinsB ? B : A;
    public bool Single { get; set; }
    // Истина, ако даден отбор участва в серията
    public bool Has(string team) => A == team || B == team;
}

// Една двойка мачове в Play-In с етикет
public class PlayInGame
{
    public string Label { get; set; } = "";
    public Series Match { get; set; } = new();
}

// Резултат от Play-In за една конференция
public class PlayInResult
{
    public List<string> Seeds { get; set; } = new();
    public List<PlayInGame> Games { get; set; } = new();
}

// Плейофна схема (bracket) за една конференция
public class Bracket
{
    public List<string> Seeds { get; set; } = new();
    public List<Series> FirstRound { get; set; } = new();
    public List<Series> Semis { get; set; } = new();
    public Series ConferenceFinal { get; set; } = new();
    public string Champion => ConferenceFinal.Winner;
    // Номерът на поставяне на отбор (0, ако не е в плейофите)
    public int SeedOf(string team) => Seeds.IndexOf(team) + 1;
}

// Един кръг от пътя на нашия отбор
public class PathRound
{
    public string Name { get; set; } = "";
    public int Round { get; set; }
    public Series Match { get; set; } = new();
}

// Награда за сезона (MVP или DPOY)
public class Award
{
    public bool IsYou { get; set; }
    public string Name { get; set; } = "";
    public string Team { get; set; } = "";
    public string Id { get; set; } = "";
    public string Line { get; set; } = "";
}

// Статистики на нашия играч за сезона
public class PlayerSeasonStats
{
    public double Ppg { get; set; }
    public double Rpg { get; set; }
    public double Apg { get; set; }
    public double Spg { get; set; }
    public double Bpg { get; set; }
    public double Tov { get; set; }
    public double Fg { get; set; }
    public double Tp { get; set; }
    public double Ft { get; set; }
    public double Per { get; set; }
}

// Копиран атрибут за показване на страницата на сезона
public class BuildLine
{
    public string Attribute { get; set; } = "";
    public string Player { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public int Value { get; set; }
}

// Целият сезон: изчислява се веднъж и се показва стъпка по стъпка
public class SeasonState
{
    public string Team { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Group { get; set; } = "";
    public int Overall { get; set; }
    public string Archetype { get; set; } = "";
    public int JerseyNumber { get; set; }
    public int Step { get; set; }
    public List<BuildLine> Build { get; set; } = new();
    public List<SeasonGame> Games { get; set; } = new();
    public Dictionary<string, int> Wins { get; set; } = new();
    public Dictionary<string, double> Strength { get; set; } = new();
    public List<string> Standings { get; set; } = new();
    public List<string> OtherStandings { get; set; } = new();
    public PlayerSeasonStats Stats { get; set; } = new();
    public Award Mvp { get; set; } = new();
    public Award Dpoy { get; set; } = new();
    public PlayInResult? PlayIn { get; set; }
    public Bracket MyBracket { get; set; } = new();
    public Bracket OtherBracket { get; set; } = new();
    public Series Finals { get; set; } = new();
    public List<PathRound> Path { get; set; } = new();
    public bool Prepared { get; set; }
    public string Source { get; set; } = "build";
    public string SalaryKey { get; set; } = "";
    public bool IsEast => Teams.Get(Team).IsEast;
    public int Seed => Standings.IndexOf(Team) + 1;
    public bool Champion => Path.Count > 0 && Path[^1].Round == 3 && Path[^1].Match.Winner == Team;
}

// Данни за една кутийка в плейофната схема (за частичния изглед _SeriesBox)
public record SeriesBox(Series? Match, List<string> Seeds, string Us, int Round);

// Данни за картата на една награда (за частичния изглед _Award): име на наградата и победителят
public record AwardBox(string Label, Award Winner);

// Един ден (или една безкрайна игра) в Salary Cap Game
public class SalaryDay
{
    public string Key { get; set; } = "";
    public int Seed { get; set; }
    public int Budget { get; set; }
    public List<List<int>> Grid { get; set; } = new();
    public Dictionary<int, int> Picks { get; set; } = new();
    public int Shuffles { get; set; } = 2;
    public int Scouts { get; set; } = 1;
    public Dictionary<int, string> Scouted { get; set; } = new();
    public string Mode { get; set; } = "";
    public List<string> Flip { get; set; } = new();
    public SalaryResult? Result { get; set; }
}

// Заключеният билд в Salary Cap Game
public class SalaryResult
{
    public List<int> Players { get; set; } = new();
    public List<int> Prices { get; set; } = new();
    public int Overall { get; set; }
    public double Ppg { get; set; }
    public double Apg { get; set; }
    public double Rpg { get; set; }
    public int Spent { get; set; }
    public string Team { get; set; } = "";
}

// Цялото състояние на Salary Cap Game в сесията на посетителя
public class SalaryState
{
    public string Current { get; set; } = "";
    public Dictionary<string, SalaryDay> Days { get; set; } = new();
    public string Menu { get; set; } = "";
    public bool Locked { get; set; }
}

// Един ред в класацията на Salary Cap Game
public class SalaryEntry
{
    public string Owner { get; set; } = "";
    public string Name { get; set; } = "";
    public List<int> Players { get; set; } = new();
    public int Overall { get; set; }
    public double Total { get; set; }
    public int Spare { get; set; }
}

// Състояние на Higher or Lower
public class HigherLowerState
{
    // Дали да се пропускат контузените играчи (от настройката "Hide injured players"; задава се при всяка заявка)
    public bool HideInjured { get; set; }
    public string Mode { get; set; } = "";
    public int Left { get; set; } = -1;
    public int Right { get; set; } = -1;
    public string Stat { get; set; } = "ppg";
    public int Score { get; set; }
    public int BestCurrent { get; set; }
    public int BestAllTime { get; set; }
    public int Best => Mode == "c" ? BestCurrent : BestAllTime;
    public string? Guess { get; set; }
    public bool Correct { get; set; }
    public bool Over { get; set; }
}

// Състояние на Guess the Player
public class GuessState
{
    public int Mystery { get; set; } = -1;
    public List<int> Guesses { get; set; } = new();
    public bool Won { get; set; }
    public bool Over { get; set; }
    public int Limit { get; set; } = 8;
    public bool OutOfGuesses { get; set; }
    public string Message { get; set; } = "";
    public int Played { get; set; }
    public int Wins { get; set; }
}

// Състояние на Position Quiz
public class QuizState
{
    public List<int> Order { get; set; } = new();
    public int Round { get; set; }
    public int Score { get; set; }
    public string? Answer { get; set; }
}

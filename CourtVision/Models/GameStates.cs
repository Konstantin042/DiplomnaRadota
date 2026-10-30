// Състоянията на игрите, които се пазят в сесията на всеки посетител
namespace CourtVision.Models; // Пространство от имена за моделите

// Състояние на играта 82-0 (драфт от 8 рунда)
public class ChallengeState
{
    // Кой играч на кое място е: "PG", "SG", "SF", "PF", "C" и резерви "B0", "B1", "B2"
    public Dictionary<string, PoolPlayer> Lineup { get; set; } = new();
    // Текущият рунд (от 1 до 8; 9 означава, че драфтът е завършен)
    public int Round { get; set; } = 1;
    // Завъртяният състав, например "BOS|60" (null, ако още няма завъртане)
    public string? CurrentKey { get; set; }
    // Оставащи респинове за отбор
    public int TeamRespins { get; set; } = 2;
    // Оставащи респинове за ера (десетилетие)
    public int EraRespins { get; set; } = 2;
    // Мястото на игрището, което играчът е избрал за преместване
    public string? SelectedSlot { get; set; }
    // Съобщение към играча след последното действие
    public string Message { get; set; } = "";
    // Резултатите от последната симулация (true = победа)
    public List<bool>? Games { get; set; }
    // Истина, ако всички 8 места са попълнени
    public bool IsComplete => Round > 8;
}

// Атрибут, копиран от реален играч
public class TakenAttribute
{
    // Номерът на играча в списъка с играчи
    public int PlayerIndex { get; set; }
    // Оценката (от 0 = F до 11 = S)
    public int Value { get; set; }
}

// Състояние на Build-a-Player
public class BuildState
{
    // Режим: "" = началното меню, "a" = All-Time, "c" = Current
    public string Mode { get; set; } = "";
    // Група: "g" = Guard, "b" = Big
    public string Group { get; set; } = "g";
    // Отвореният таб в арената: skills, physical или mental
    public string Tab { get; set; } = "skills";
    // Завъртяният отбор
    public string? Team { get; set; }
    // Номерът на завъртяния играч (null, ако няма)
    public int? Current { get; set; }
    // Оставащ респин за отбор
    public int TeamRespins { get; set; } = 1;
    // Оставащ респин за играч
    public int PlayerRespins { get; set; } = 1;
    // Попълнените атрибути: номер на атрибута → копирана оценка
    public Dictionary<int, TakenAttribute> Taken { get; set; } = new();
    // Номерата на вече използваните играчи (да не се падат пак)
    public HashSet<int> Used { get; set; } = new();
    // Истина, когато са попълнени всичките 10 атрибута
    public bool Done => Taken.Count == 10;
}

// Един мач от редовния сезон
public class SeasonGame
{
    // Противникът (съкращение на отбора)
    public string Opponent { get; set; } = "";
    // Истина при победа
    public bool Win { get; set; }
}

// Серия между два отбора (до 4 победи, или 1 мач в Play-In)
public class Series
{
    // Първият отбор (по-високо поставен)
    public string A { get; set; } = "";
    // Вторият отбор
    public string B { get; set; } = "";
    // Резултат от всеки мач: true = победа за A
    public List<bool> Games { get; set; } = new();
    // Точки на A във всеки мач
    public List<int> ScoresA { get; set; } = new();
    // Точки на B във всеки мач
    public List<int> ScoresB { get; set; } = new();
    // Победи на A в серията
    public int WinsA => Games.Count(g => g);
    // Победи на B в серията
    public int WinsB => Games.Count(g => !g);
    // Победителят в серията
    public string Winner => WinsA > WinsB ? A : B;
    // Загубилият в серията
    public string Loser => WinsA > WinsB ? B : A;
    // Истина, ако е само един мач (Play-In)
    public bool Single { get; set; }
    // Истина, ако даден отбор участва в серията
    public bool Has(string team) => A == team || B == team;
}

// Една двойка мачове в Play-In с етикет
public class PlayInGame
{
    // Описание, например "7 vs 8"
    public string Label { get; set; } = "";
    // Самият мач
    public Series Match { get; set; } = new();
}

// Резултат от Play-In за една конференция
public class PlayInResult
{
    // Окончателните 8 поставени отбора след Play-In
    public List<string> Seeds { get; set; } = new();
    // Трите мача от Play-In
    public List<PlayInGame> Games { get; set; } = new();
}

// Плейофна схема (bracket) за една конференция
public class Bracket
{
    // Поставените отбори 1–8
    public List<string> Seeds { get; set; } = new();
    // Първи кръг: 4 серии
    public List<Series> FirstRound { get; set; } = new();
    // Полуфинали в конференцията: 2 серии
    public List<Series> Semis { get; set; } = new();
    // Финал на конференцията
    public Series ConferenceFinal { get; set; } = new();
    // Шампионът на конференцията
    public string Champion => ConferenceFinal.Winner;
    // Номерът на поставяне на отбор (0, ако не е в плейофите)
    public int SeedOf(string team) => Seeds.IndexOf(team) + 1;
}

// Един кръг от пътя на нашия отбор
public class PathRound
{
    // Име на кръга, например "First Round"
    public string Name { get; set; } = "";
    // Номер на кръга: -1 = Play-In, 0 = първи кръг ... 3 = финали
    public int Round { get; set; }
    // Серията, която играем
    public Series Match { get; set; } = new();
}

// Награда за сезона (MVP или DPOY)
public class Award
{
    // Истина, ако наградата е за нашия играч
    public bool IsYou { get; set; }
    // Име на победителя
    public string Name { get; set; } = "";
    // Отбор на победителя
    public string Team { get; set; } = "";
    // Идентификатор за снимката
    public string Id { get; set; } = "";
    // Ред със статистики
    public string Line { get; set; } = "";
}

// Статистики на нашия играч за сезона
public class PlayerSeasonStats
{
    public double Ppg { get; set; }  // Точки на мач
    public double Rpg { get; set; }  // Борби на мач
    public double Apg { get; set; }  // Асистенции на мач
    public double Spg { get; set; }  // Откраднати топки на мач
    public double Bpg { get; set; }  // Блокади на мач
    public double Tov { get; set; }  // Загубени топки на мач
    public double Fg { get; set; }   // Процент от стрелбата от игра
    public double Tp { get; set; }   // Процент от тройки (0 = не стреля тройки)
    public double Ft { get; set; }   // Процент от наказателни удари
    public double Per { get; set; }  // Рейтинг на ефективност (PER)
}

// Копиран атрибут за показване на страницата на сезона
public class BuildLine
{
    public string Attribute { get; set; } = ""; // Име на атрибута
    public string Player { get; set; } = "";    // От кой играч е копиран
    public string PlayerId { get; set; } = "";  // Идентификатор за снимката
    public int Value { get; set; }              // Оценката
}

// Целият сезон: изчислява се веднъж и се показва стъпка по стъпка
public class SeasonState
{
    public string Team { get; set; } = "";                    // Нашият отбор
    public string Mode { get; set; } = "";                    // All-Time или Current
    public string Group { get; set; } = "";                   // Guard или Big
    public int Overall { get; set; }                          // OVR на нашия играч
    public string Archetype { get; set; } = "";               // Тип на играча
    public int JerseyNumber { get; set; }                     // Номер на фланелката
    public int Step { get; set; }                             // 0 отбор, 1 сезон, 2 плейофи, 3 отчет
    public List<BuildLine> Build { get; set; } = new();       // Копираните атрибути
    public List<SeasonGame> Games { get; set; } = new();      // 82-та мача
    public Dictionary<string, int> Wins { get; set; } = new(); // Победи на всеки отбор
    public Dictionary<string, double> Strength { get; set; } = new(); // Сила на всеки отбор
    public List<string> Standings { get; set; } = new();      // Класиране в нашата конференция
    public List<string> OtherStandings { get; set; } = new(); // Класиране в другата конференция
    public PlayerSeasonStats Stats { get; set; } = new();     // Статистиките ни
    public Award Mvp { get; set; } = new();                   // Най-полезен играч
    public Award Dpoy { get; set; } = new();                  // Най-добър защитник
    public PlayInResult? PlayIn { get; set; }                 // Play-In на нашата конференция (ако сме 7–10)
    public Bracket MyBracket { get; set; } = new();           // Схемата на нашата конференция
    public Bracket OtherBracket { get; set; } = new();        // Схемата на другата конференция
    public Series Finals { get; set; } = new();               // Финалите на NBA
    public List<PathRound> Path { get; set; } = new();        // Сериите, които играем
    public bool Prepared { get; set; }                        // Истина, след като плейофите са изчислени
    public string Source { get; set; } = "build";             // Откъде е билдът: "build" (Build-a-Player) или "salary" (Salary Cap Game)
    public string SalaryKey { get; set; } = "";               // За Salary Cap Game: денят ("2026-10-27") или "inf"
    public bool IsEast => Teams.Get(Team).IsEast;             // Истина, ако сме от Изтока
    public int Seed => Standings.IndexOf(Team) + 1;           // Нашето място в конференцията
    // Истина, ако сме спечелили финалите
    public bool Champion => Path.Count > 0 && Path[^1].Round == 3 && Path[^1].Match.Winner == Team;
}

// Данни за една кутийка в плейофната схема (за частичния изглед _SeriesBox)
public record SeriesBox(Series? Match, List<string> Seeds, string Us, int Round);

// Данни за картата на една награда (за частичния изглед _Award): име на наградата и победителят
public record AwardBox(string Label, Award Winner);

// Един ден (или една безкрайна игра) в Salary Cap Game
public class SalaryDay
{
    public string Key { get; set; } = "";                       // Ключ: дата "2026-10-27" или "inf"
    public int Seed { get; set; }                               // Число, от което се генерира мрежата (еднакво за всички в един ден)
    public int Budget { get; set; }                             // Таванът на заплатите за деня ($130M–$170M)
    public List<List<int>> Grid { get; set; } = new();          // Мрежата 5×5: [колона][ред] → номер на играча
    public Dictionary<int, int> Picks { get; set; } = new();    // Избраните играчи: колона → ред
    public int Shuffles { get; set; } = 2;                      // Оставащи разбърквания (rerolls)
    public int Scouts { get; set; } = 1;                        // Оставащи скаутвания
    public Dictionary<int, string> Scouted { get; set; } = new(); // Скаутвани играчи: номер на играча → оценка (буква)
    public string Mode { get; set; } = "";                      // Режим: "" (избор), "shuffle" (разбъркване) или "scout" (скаут)
    public List<string> Flip { get; set; } = new();             // Клетките, които току-що са сменени ("колона-ред"), за анимацията
    public SalaryResult? Result { get; set; }                   // Резултатът, след като билдът е заключен (null преди това)
}

// Заключеният билд в Salary Cap Game
public class SalaryResult
{
    public List<int> Players { get; set; } = new();  // Избраните играчи по колони
    public List<int> Prices { get; set; } = new();   // Цената на всеки от тях (в милиони)
    public int Overall { get; set; }                 // Обща оценка: сбор от най-силния атрибут на всеки играч
    public double Ppg { get; set; }                  // Точки на мач
    public double Apg { get; set; }                  // Асистенции на мач
    public double Rpg { get; set; }                  // Борби на мач
    public int Spent { get; set; }                   // Похарчени милиони
    public string Team { get; set; } = "";           // Отборът, за който играе билдът
}

// Цялото състояние на Salary Cap Game в сесията на посетителя
public class SalaryState
{
    public string Current { get; set; } = "";                              // Кой ден е отворен ("" = днес)
    public Dictionary<string, SalaryDay> Days { get; set; } = new();       // Всички изиграни или започнати дни
    public string Menu { get; set; } = "";                                 // Отворено меню: "" , "power", "dates" или "board"
    public bool Locked { get; set; }                                       // Истина за кратко съобщение "Play today's game to unlock"
}

// Един ред в класацията на Salary Cap Game
public class SalaryEntry
{
    public string Owner { get; set; } = "";          // Идентификатор на сесията (за да знаем кой ред е наш)
    public string Name { get; set; } = "";           // Име за показване, например "Guest 4821"
    public List<int> Players { get; set; } = new();  // Избраните играчи
    public int Overall { get; set; }                 // Обща оценка
    public double Total { get; set; }                // PPG + APG + RPG (при равен резултат)
    public int Spare { get; set; }                   // Неизхарчени милиони
}

// Състояние на Higher or Lower
public class HigherLowerState
{
    public string Mode { get; set; } = "";       // Режим: "" (меню), "c" (Current – сезон 2025–26) или "a" (All-Time – всички десетилетия)
    public int Left { get; set; } = -1;          // Играчът вляво (номер в списъка); стойността му се вижда
    public int Right { get; set; } = -1;         // Играчът вдясно; стойността му е скрита до отговора
    public string Stat { get; set; } = "ppg";    // Показателят в този рунд: ppg, rpg или apg
    public int Score { get; set; }               // Поредни верни отговори
    public int BestCurrent { get; set; }         // Най-добрата серия в режим Current (в тази сесия)
    public int BestAllTime { get; set; }         // Най-добрата серия в режим All-Time (в тази сесия)
    public int Best => Mode == "c" ? BestCurrent : BestAllTime; // Рекордът за текущия режим
    public string? Guess { get; set; }           // Отговорът: "higher", "lower" или null (още няма)
    public bool Correct { get; set; }            // Истина, ако отговорът е верен
    public bool Over { get; set; }               // Истина, когато играта е свършила (грешен отговор)
}

// Състояние на Guess the Player
public class GuessState
{
    public int Mystery { get; set; } = -1;               // Тайният играч (номер в списъка)
    public List<int> Guesses { get; set; } = new();      // Опитите досега (номера на играчи), най-новият е последен
    public bool Won { get; set; }                        // Истина, ако сме познали
    public bool Over { get; set; }                       // Истина, когато играта е свършила (познали или показан отговор)
    public int Limit { get; set; } = 8;                  // Колко опита има в момента (расте с 4 при "Keep guessing")
    public bool OutOfGuesses { get; set; }               // Истина, когато опитите са свършили, но отговорът още е скрит
    public string Message { get; set; } = "";            // Съобщение след последния опит (например "Player not found")
    public int Played { get; set; }                      // Изиграни игри в тази сесия
    public int Wins { get; set; }                        // Спечелени игри в тази сесия
}

// Състояние на Position Quiz
public class QuizState
{
    // Номерата на 8-те играча за този куиз
    public List<int> Order { get; set; } = new();
    // Текущият рунд (0 до 7; 8 = край)
    public int Round { get; set; }
    // Точки (верни отговори)
    public int Score { get; set; }
    // Отговорът в текущия рунд (null, ако още няма отговор)
    public string? Answer { get; set; }
}

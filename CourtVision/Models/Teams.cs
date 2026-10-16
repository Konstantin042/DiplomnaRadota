// Информация за 30-те отбора в NBA
namespace CourtVision.Models; // Пространство от имена за всички модели

// Един отбор: съкращение, пълно име, основен цвят и конференция
public record TeamInfo(string Abbr, string Name, string Color, string Conference)
{
    // Само прякорът на отбора, например "Lakers" от "Los Angeles Lakers"
    public string Nickname => Name.Split(' ').Last();
    // Истина, ако отборът е от Източната конференция
    public bool IsEast => Conference == "East";
}

// Статичен каталог с всички отбори
public static class Teams
{
    // Списък с всички 30 отбора (съкращение, име, цвят, конференция)
    public static readonly IReadOnlyList<TeamInfo> All = new List<TeamInfo>
    {
        new("ATL", "Atlanta Hawks", "#E03A3E", "East"),          // Атланта
        new("BOS", "Boston Celtics", "#007A33", "East"),         // Бостън
        new("BKN", "Brooklyn Nets", "#4B5563", "East"),          // Бруклин
        new("CHA", "Charlotte Hornets", "#1D8CAB", "East"),      // Шарлът
        new("CHI", "Chicago Bulls", "#CE1141", "East"),          // Чикаго
        new("CLE", "Cleveland Cavaliers", "#860038", "East"),    // Кливланд
        new("DAL", "Dallas Mavericks", "#00538C", "West"),       // Далас
        new("DEN", "Denver Nuggets", "#0E2240", "West"),         // Денвър
        new("DET", "Detroit Pistons", "#C8102E", "East"),        // Детройт
        new("GSW", "Golden State Warriors", "#1D428A", "West"),  // Голдън Стейт
        new("HOU", "Houston Rockets", "#CE1141", "West"),        // Хюстън
        new("IND", "Indiana Pacers", "#002D62", "East"),         // Индиана
        new("LAC", "LA Clippers", "#C8102E", "West"),            // Клипърс
        new("LAL", "Los Angeles Lakers", "#552583", "West"),     // Лейкърс
        new("MEM", "Memphis Grizzlies", "#5D76A9", "West"),      // Мемфис
        new("MIA", "Miami Heat", "#98002E", "East"),             // Маями
        new("MIL", "Milwaukee Bucks", "#00471B", "East"),        // Милуоки
        new("MIN", "Minnesota Timberwolves", "#236192", "West"), // Минесота
        new("NOP", "New Orleans Pelicans", "#0C2340", "West"),   // Ню Орлиънс
        new("NYK", "New York Knicks", "#006BB6", "East"),        // Ню Йорк
        new("OKC", "Oklahoma City Thunder", "#007AC1", "West"),  // Оклахома
        new("ORL", "Orlando Magic", "#0077C0", "East"),          // Орландо
        new("PHI", "Philadelphia 76ers", "#006BB6", "East"),     // Филаделфия
        new("PHX", "Phoenix Suns", "#1D1160", "West"),           // Финикс
        new("POR", "Portland Trail Blazers", "#E03A3E", "West"), // Портланд
        new("SAC", "Sacramento Kings", "#5A2D81", "West"),       // Сакраменто
        new("SAS", "San Antonio Spurs", "#6B7280", "West"),      // Сан Антонио
        new("TOR", "Toronto Raptors", "#CE1141", "East"),        // Торонто
        new("UTA", "Utah Jazz", "#3E2680", "West"),              // Юта
        new("WAS", "Washington Wizards", "#002B5C", "East"),     // Вашингтон
    };

    // Речник за бързо търсене на отбор по съкращение
    private static readonly Dictionary<string, TeamInfo> ByAbbr = All.ToDictionary(t => t.Abbr);

    // Връща отбора по съкращение (или празен отбор, ако не съществува)
    public static TeamInfo Get(string abbr) =>
        ByAbbr.TryGetValue(abbr, out var team) ? team : new TeamInfo(abbr, abbr, "#334155", "East");

    // Съкращенията на отборите от Източната конференция
    public static IReadOnlyList<string> East => All.Where(t => t.IsEast).Select(t => t.Abbr).ToList();
    // Съкращенията на отборите от Западната конференция
    public static IReadOnlyList<string> West => All.Where(t => !t.IsEast).Select(t => t.Abbr).ToList();
}

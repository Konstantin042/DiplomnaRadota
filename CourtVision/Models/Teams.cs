// Информация за 30-те отбора в NBA
namespace CourtVision.Models;

// Един отбор: съкращение, пълно име, основен цвят и конференция
public record TeamInfo(string Abbr, string Name, string Color, string Conference)
{
    public string Nickname => Name.Split(' ').Last();
    public bool IsEast => Conference == "East";
}

// Статичен каталог с всички отбори
public static class Teams
{
    public static readonly IReadOnlyList<TeamInfo> All = new List<TeamInfo>
    {
        new("ATL", "Atlanta Hawks", "#E03A3E", "East"),
        new("BOS", "Boston Celtics", "#007A33", "East"),
        new("BKN", "Brooklyn Nets", "#4B5563", "East"),
        new("CHA", "Charlotte Hornets", "#1D8CAB", "East"),
        new("CHI", "Chicago Bulls", "#CE1141", "East"),
        new("CLE", "Cleveland Cavaliers", "#860038", "East"),
        new("DAL", "Dallas Mavericks", "#00538C", "West"),
        new("DEN", "Denver Nuggets", "#0E2240", "West"),
        new("DET", "Detroit Pistons", "#C8102E", "East"),
        new("GSW", "Golden State Warriors", "#1D428A", "West"),
        new("HOU", "Houston Rockets", "#CE1141", "West"),
        new("IND", "Indiana Pacers", "#002D62", "East"),
        new("LAC", "LA Clippers", "#C8102E", "West"),
        new("LAL", "Los Angeles Lakers", "#552583", "West"),
        new("MEM", "Memphis Grizzlies", "#5D76A9", "West"),
        new("MIA", "Miami Heat", "#98002E", "East"),
        new("MIL", "Milwaukee Bucks", "#00471B", "East"),
        new("MIN", "Minnesota Timberwolves", "#236192", "West"),
        new("NOP", "New Orleans Pelicans", "#0C2340", "West"),
        new("NYK", "New York Knicks", "#006BB6", "East"),
        new("OKC", "Oklahoma City Thunder", "#007AC1", "West"),
        new("ORL", "Orlando Magic", "#0077C0", "East"),
        new("PHI", "Philadelphia 76ers", "#006BB6", "East"),
        new("PHX", "Phoenix Suns", "#1D1160", "West"),
        new("POR", "Portland Trail Blazers", "#E03A3E", "West"),
        new("SAC", "Sacramento Kings", "#5A2D81", "West"),
        new("SAS", "San Antonio Spurs", "#6B7280", "West"),
        new("TOR", "Toronto Raptors", "#CE1141", "East"),
        new("UTA", "Utah Jazz", "#3E2680", "West"),
        new("WAS", "Washington Wizards", "#002B5C", "East"),
    };

    private static readonly Dictionary<string, TeamInfo> ByAbbr = All.ToDictionary(t => t.Abbr);

    // Връща отбора по съкращение (или празен отбор, ако не съществува)
    public static TeamInfo Get(string abbr) =>
        ByAbbr.TryGetValue(abbr, out var team) ? team : new TeamInfo(abbr, abbr, "#334155", "East");

    public static IReadOnlyList<string> East => All.Where(t => t.IsEast).Select(t => t.Abbr).ToList();
    public static IReadOnlyList<string> West => All.Where(t => !t.IsEast).Select(t => t.Abbr).ToList();
}

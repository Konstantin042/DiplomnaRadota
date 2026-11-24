// Данните за страницата на един отбор (/Teams/Details)
using CourtVision.Database;

namespace CourtVision.Models;

// Един играч от отбора за списъка с най-добрите реализатори
public record TeamScorer(string Name, string Code, string Position, string Decade, double Ppg, double Rpg, double Apg);

// Данните за страницата на един отбор
public record TeamPage(Team Team, int PlayerCount, List<TeamScorer> TopScorers, List<CurrentPlayer> Roster);

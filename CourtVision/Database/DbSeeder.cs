// Попълва базата данни при първо стартиране: прехвърля отборите и играчите от JSON файловете в таблиците
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Database;

// Статичен клас с един метод: Seed
public static class DbSeeder
{
    // Попълва всяка празна таблица; ако таблицата вече има данни, не я пипа
    public static void Seed(AppDbContext db, string dataDir)
    {
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        if (!db.Teams.Any()) SeedTeams(db);
        FillTeamDetails(db, Path.Combine(dataDir, "teams.json"));
        if (!db.RosterPlayers.Any()) SeedRosters(db, Path.Combine(dataDir, "pools.json"));
        if (!db.RatedPlayers.Any()) SeedRatedPlayers(db, Path.Combine(dataDir, "build-players.json"));
        if (!db.QuizEntries.Any()) SeedQuiz(db, Path.Combine(dataDir, "quiz-players.json"));
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    // Записва 30-те отбора
    private static void SeedTeams(AppDbContext db)
    {
        foreach (var t in CourtVision.Models.Teams.All)
            db.Teams.Add(new Team { Abbr = t.Abbr, Name = t.Name, Color = t.Color, Conference = t.Conference });
        // Записваме в базата
        db.SaveChanges();
    }

    // Попълва града, залата, титлите и историята на отборите от teams.json.
    // Ако базата е създадена от по-стара версия, първо добавя новите колони (без да трие потребителите и резултатите)
    private static void FillTeamDetails(AppDbContext db, string path)
    {
        if (!File.Exists(path)) return;
        AddMissingTeamColumns(db);
        if (!db.Teams.Any(t => t.Arena == "")) return;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var teams = db.Teams.ToDictionary(t => t.Abbr);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (!teams.TryGetValue(row.GetProperty("abbr").GetString() ?? "", out var team)) continue;
            team.City = row.GetProperty("city").GetString() ?? "";
            team.Arena = row.GetProperty("arena").GetString() ?? "";
            team.Division = row.GetProperty("division").GetString() ?? "";
            team.JoinedYear = row.GetProperty("joined").GetInt32();
            team.Titles = string.Join(",", row.GetProperty("titles").EnumerateArray().Select(y => y.GetInt32()));
            team.Legends = string.Join("|", row.GetProperty("legends").EnumerateArray().Select(n => n.GetString()));
            team.History = row.GetProperty("history").GetString() ?? "";
            db.Entry(team).State = EntityState.Modified;
        }
        db.SaveChanges();
    }

    // EnsureCreated не променя вече съществуваща база, затова новите колони се добавят с ALTER TABLE
    private static void AddMissingTeamColumns(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Teams')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) existing.Add(reader.GetString(1));
        }
        var columns = new Dictionary<string, string>
        {
            ["Division"] = "TEXT NOT NULL DEFAULT ''",
            ["City"] = "TEXT NOT NULL DEFAULT ''",
            ["Arena"] = "TEXT NOT NULL DEFAULT ''",
            ["JoinedYear"] = "INTEGER NOT NULL DEFAULT 0",
            ["Titles"] = "TEXT NOT NULL DEFAULT ''",
            ["Legends"] = "TEXT NOT NULL DEFAULT ''",
            ["History"] = "TEXT NOT NULL DEFAULT ''",
        };
        foreach (var (name, type) in columns)
        {
            if (existing.Contains(name)) continue;
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"ALTER TABLE Teams ADD COLUMN {name} {type}";
            cmd.ExecuteNonQuery();
        }
        connection.Close();
    }

    // Чете pools.json: {"BOS|60": [[име, id, "SF/SG", ppg, rpg, apg, spg, bpg], ...]} и записва RosterPlayers
    private static void SeedRosters(AppDbContext db, string path)
    {
        if (!File.Exists(path)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var order = 0;
        foreach (var pool in doc.RootElement.EnumerateObject())
        {
            var parts = pool.Name.Split('|');
            foreach (var row in pool.Value.EnumerateArray())
            {
                db.RosterPlayers.Add(new RosterPlayer
                {
                    SortOrder = order++,
                    Name = row[0].GetString() ?? "",
                    PlayerCode = row[1].GetString() ?? "",
                    Positions = row[2].GetString() ?? "PG",
                    TeamAbbr = parts[0],
                    Decade = parts[1],
                    Ppg = row[3].GetDouble(),
                    Rpg = row[4].GetDouble(),
                    Apg = row[5].GetDouble(),
                    Spg = row[6].ValueKind == JsonValueKind.Null ? null : row[6].GetDouble(),
                    Bpg = row[7].ValueKind == JsonValueKind.Null ? null : row[7].GetDouble(),
                });
            }
        }
        // Записваме всички редове наведнъж
        db.SaveChanges();
    }

    // Чете build-players.json: [[режим, група, име, отбор, години, ръст, тегло, id, новобранец, [оценки], [статистики]], ...]
    private static void SeedRatedPlayers(AppDbContext db, string path)
    {
        if (!File.Exists(path)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var order = 0;
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            var stats = row[10].EnumerateArray().Select(v => v.GetDouble()).ToArray();
            var player = new RatedPlayer
            {
                SortOrder = order++,
                Mode = row[0].GetString() ?? "",
                Group = row[1].GetString() ?? "",
                Name = row[2].GetString() ?? "",
                TeamAbbr = row[3].GetString() ?? "",
                Years = row[4].GetString() ?? "",
                Height = row[5].GetInt32(),
                Weight = row[6].GetInt32(),
                PlayerCode = row[7].GetString() ?? "",
                Rookie = row[8].GetInt32() == 1,
                Ppg = stats.Length == 5 ? stats[0] : null,
                Rpg = stats.Length == 5 ? stats[1] : null,
                Apg = stats.Length == 5 ? stats[2] : null,
                Spg = stats.Length == 5 ? stats[3] : null,
                Bpg = stats.Length == 5 ? stats[4] : null,
            };
            var slot = 0;
            foreach (var grade in row[9].EnumerateArray())
                player.Grades.Add(new PlayerGrade { Slot = slot++, Value = grade.GetInt32() });
            db.RatedPlayers.Add(player);
        }
        // Записваме всички редове наведнъж
        db.SaveChanges();
    }

    // Чете quiz-players.json и записва QuizEntries
    private static void SeedQuiz(AppDbContext db, string path)
    {
        if (!File.Exists(path)) return;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var players = JsonSerializer.Deserialize<List<CourtVision.Models.QuizPlayer>>(File.ReadAllText(path), options) ?? new();
        foreach (var p in players)
        {
            db.QuizEntries.Add(new QuizEntry
            {
                Name = p.Name,
                Positions = string.Join("/", p.Positions),
                TeamAbbr = p.Team,
                Decade = p.Decade,
                Overall = p.Overall,
                JumpShot = p.Skills[0],
                Handles = p.Skills[1],
                Finishing = p.Skills[2],
                Defense = p.Skills[3],
                Size = p.Skills[4],
                Ppg = p.Stats[0],
                Rpg = p.Stats[1],
                Apg = p.Stats[2],
                Spg = p.Stats[3],
                Bpg = p.Stats[4],
            });
        }
        // Записваме всички редове
        db.SaveChanges();
    }
}

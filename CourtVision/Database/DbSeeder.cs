// Попълва базата данни при първо стартиране: прехвърля отборите и играчите от JSON файловете в таблиците
using System.Text.Json; // За четене на JSON файловете

namespace CourtVision.Database; // Пространство от имена за базата данни

// Статичен клас с един метод: Seed
public static class DbSeeder
{
    // Попълва всяка празна таблица; ако таблицата вече има данни, не я пипа
    public static void Seed(AppDbContext db, string dataDir)
    {
        // По-бързо добавяне на много редове: не следим промените след всеки ред
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        // 1. Отборите (от каталога в Models/Teams.cs)
        if (!db.Teams.Any()) SeedTeams(db);
        // 2. Съставите по отбор и десетилетие
        if (!db.RosterPlayers.Any()) SeedRosters(db, Path.Combine(dataDir, "pools.json"));
        // 3. Оценените играчи и техните оценки
        if (!db.RatedPlayers.Any()) SeedRatedPlayers(db, Path.Combine(dataDir, "build-players.json"));
        // 4. Играчите за куиза
        if (!db.QuizEntries.Any()) SeedQuiz(db, Path.Combine(dataDir, "quiz-players.json"));
        // Връщаме нормалното поведение
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    // Записва 30-те отбора
    private static void SeedTeams(AppDbContext db)
    {
        // Обхождаме каталога с отборите
        foreach (var t in CourtVision.Models.Teams.All)
            // Добавяме ред в таблицата Teams
            db.Teams.Add(new Team { Abbr = t.Abbr, Name = t.Name, Color = t.Color, Conference = t.Conference });
        // Записваме в базата
        db.SaveChanges();
    }

    // Чете pools.json: {"BOS|60": [[име, id, "SF/SG", ppg, rpg, apg, spg, bpg], ...]} и записва RosterPlayers
    private static void SeedRosters(AppDbContext db, string path)
    {
        // Ако файлът липсва, няма какво да записваме
        if (!File.Exists(path)) return;
        // Отваряме JSON документа
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // Брояч за поредния номер
        var order = 0;
        // Обхождаме всеки състав (ключът е "ОТБОР|десетилетие")
        foreach (var pool in doc.RootElement.EnumerateObject())
        {
            // Разделяме ключа на отбор и десетилетие
            var parts = pool.Name.Split('|');
            // Обхождаме всеки играч в състава
            foreach (var row in pool.Value.EnumerateArray())
            {
                // Добавяме ред в таблицата RosterPlayers
                db.RosterPlayers.Add(new RosterPlayer
                {
                    SortOrder = order++,                                   // Пореден номер
                    Name = row[0].GetString() ?? "",                       // Име
                    PlayerCode = row[1].GetString() ?? "",                 // Идентификатор
                    Positions = row[2].GetString() ?? "PG",                // Позиции
                    TeamAbbr = parts[0],                                   // Отбор
                    Decade = parts[1],                                     // Десетилетие
                    Ppg = row[3].GetDouble(),                              // Точки
                    Rpg = row[4].GetDouble(),                              // Борби
                    Apg = row[5].GetDouble(),                              // Асистенции
                    Spg = row[6].ValueKind == JsonValueKind.Null ? null : row[6].GetDouble(), // Кражби или NULL
                    Bpg = row[7].ValueKind == JsonValueKind.Null ? null : row[7].GetDouble(), // Блокади или NULL
                });
            }
        }
        // Записваме всички редове наведнъж
        db.SaveChanges();
    }

    // Чете build-players.json: [[режим, група, име, отбор, години, ръст, тегло, id, новобранец, [оценки], [статистики]], ...]
    private static void SeedRatedPlayers(AppDbContext db, string path)
    {
        // Ако файлът липсва, няма какво да записваме
        if (!File.Exists(path)) return;
        // Отваряме JSON документа
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // Брояч за поредния номер
        var order = 0;
        // Обхождаме всеки ред (един играч)
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            // Реалните статистики (празен масив за новобранците)
            var stats = row[10].EnumerateArray().Select(v => v.GetDouble()).ToArray();
            // Новият играч
            var player = new RatedPlayer
            {
                SortOrder = order++,                                   // Пореден номер
                Mode = row[0].GetString() ?? "",                       // Режим
                Group = row[1].GetString() ?? "",                      // Група
                Name = row[2].GetString() ?? "",                       // Име
                TeamAbbr = row[3].GetString() ?? "",                   // Отбор
                Years = row[4].GetString() ?? "",                      // Години
                Height = row[5].GetInt32(),                            // Ръст
                Weight = row[6].GetInt32(),                            // Тегло
                PlayerCode = row[7].GetString() ?? "",                 // Идентификатор
                Rookie = row[8].GetInt32() == 1,                       // Новобранец ли е
                Ppg = stats.Length == 5 ? stats[0] : null,             // Точки или NULL
                Rpg = stats.Length == 5 ? stats[1] : null,             // Борби или NULL
                Apg = stats.Length == 5 ? stats[2] : null,             // Асистенции или NULL
                Spg = stats.Length == 5 ? stats[3] : null,             // Кражби или NULL
                Bpg = stats.Length == 5 ? stats[4] : null,             // Блокади или NULL
            };
            // Номерът на атрибута
            var slot = 0;
            // Всяка от 10-те оценки става ред в таблицата PlayerGrades
            foreach (var grade in row[9].EnumerateArray())
                player.Grades.Add(new PlayerGrade { Slot = slot++, Value = grade.GetInt32() });
            // Добавяме играча (оценките се добавят заедно с него)
            db.RatedPlayers.Add(player);
        }
        // Записваме всички редове наведнъж
        db.SaveChanges();
    }

    // Чете quiz-players.json и записва QuizEntries
    private static void SeedQuiz(AppDbContext db, string path)
    {
        // Ако файлът липсва, няма какво да записваме
        if (!File.Exists(path)) return;
        // Настройка: имената в JSON не зависят от главни/малки букви
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        // Четем списъка с играчите (използваме модела от Models)
        var players = JsonSerializer.Deserialize<List<CourtVision.Models.QuizPlayer>>(File.ReadAllText(path), options) ?? new();
        // Обхождаме играчите
        foreach (var p in players)
        {
            // Добавяме ред в таблицата QuizEntries
            db.QuizEntries.Add(new QuizEntry
            {
                Name = p.Name,                                   // Име
                Positions = string.Join("/", p.Positions),       // Позиции, например "PG/SG"
                TeamAbbr = p.Team,                               // Отбор
                Decade = p.Decade,                               // Десетилетие
                Overall = p.Overall,                             // Обща оценка
                JumpShot = p.Skills[0],                          // Стрелба
                Handles = p.Skills[1],                           // Дрибъл
                Finishing = p.Skills[2],                         // Завършване
                Defense = p.Skills[3],                           // Защита
                Size = p.Skills[4],                              // Ръст и сила
                Ppg = p.Stats[0],                                // Точки
                Rpg = p.Stats[1],                                // Борби
                Apg = p.Stats[2],                                // Асистенции
                Spg = p.Stats[3],                                // Кражби
                Bpg = p.Stats[4],                                // Блокади
            });
        }
        // Записваме всички редове
        db.SaveChanges();
    }
}

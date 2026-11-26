// Контузените играчи: пази последния доклад в паметта, за да могат игрите бързо да проверяват дали някой е контузен
using System.Globalization;
using System.Text;
using System.Text.Json;
using CourtVision.Database;
using Microsoft.EntityFrameworkCore;

namespace CourtVision.Services;

public record Injury(string Name, string Team, string Status, string Detail);

public class InjuryService
{
    // Безплатният доклад за контузиите на ESPN (без ключ)
    public const string FeedUrl = "https://site.api.espn.com/apis/site/v2/sports/basketball/nba/injuries";
    public const string CookieName = "cv-hide-injured";

    private Dictionary<string, Injury> _byName = new();
    public DateTime? UpdatedAt { get; private set; }
    public string? LastError { get; private set; }

    // Името за сравнение: без диакритика, точки, апострофи и Jr./III
    public static string Normalize(string name)
    {
        var plain = new string(name.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var words = plain.ToLowerInvariant().Replace(".", "").Replace("'", "").Replace(",", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w is not ("jr" or "sr" or "ii" or "iii" or "iv"));
        return string.Join(' ', words);
    }

    public Injury? Find(string name) => _byName.GetValueOrDefault(Normalize(name));

    // "Out" (и "Out For Season") се махат от игрите; "Day-To-Day" само получава значка
    public static bool IsOut(Injury? i) => i != null && i.Status.StartsWith("Out", StringComparison.OrdinalIgnoreCase);
    public bool IsOut(string name) => IsOut(Find(name));

    public IReadOnlyCollection<Injury> All => _byName.Values;

    // Дали посетителят е включил "Hide injured players" в настройките
    public static bool HideFor(HttpRequest request) => request.Cookies[CookieName] == "1";

    // Чете отговора на ESPN: { injuries: [ { displayName: отбор, injuries: [ { status, athlete: { displayName } } ] } ] }
    public static List<Injury> Parse(string json)
    {
        var list = new List<Injury>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("injuries", out var teams)) return list;
        foreach (var team in teams.EnumerateArray())
        {
            var teamName = team.TryGetProperty("displayName", out var tn) ? tn.GetString() ?? "" : "";
            if (!team.TryGetProperty("injuries", out var entries)) continue;
            foreach (var e in entries.EnumerateArray())
            {
                if (!e.TryGetProperty("athlete", out var athlete) || !athlete.TryGetProperty("displayName", out var dn)) continue;
                var status = e.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                var detail = e.TryGetProperty("shortComment", out var sc) ? sc.GetString() ?? "" : "";
                list.Add(new Injury(dn.GetString() ?? "", teamName, status, detail.Length > 200 ? detail[..200] : detail));
            }
        }
        return list;
    }

    // Записва новия доклад в паметта и в таблицата InjuredPlayers (добавя, променя и трие редове)
    public void Apply(List<Injury> injuries, AppDbContext db)
    {
        var fresh = new Dictionary<string, Injury>();
        foreach (var i in injuries) fresh[Normalize(i.Name)] = i;

        var rows = db.InjuredPlayers.ToList();
        foreach (var row in rows.Where(r => !fresh.ContainsKey(r.NormName)))
            db.InjuredPlayers.Remove(row);                                   // Delete: играчът се е възстановил
        foreach (var (norm, i) in fresh)
        {
            var row = rows.FirstOrDefault(r => r.NormName == norm);
            if (row == null)
                db.InjuredPlayers.Add(new InjuredPlayer { PlayerName = i.Name, NormName = norm, TeamName = i.Team, Status = i.Status, Detail = i.Detail, UpdatedAt = DateTime.UtcNow });   // Create
            else if (row.Status != i.Status || row.Detail != i.Detail)
            {
                row.Status = i.Status; row.Detail = i.Detail; row.TeamName = i.Team; row.UpdatedAt = DateTime.UtcNow;   // Update
                db.Entry(row).State = EntityState.Modified;
            }
        }
        db.SaveChanges();

        _byName = fresh;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    // При стартиране: зарежда последния запазен доклад от базата (работи и без интернет)
    public void LoadFromDb(AppDbContext db)
    {
        var rows = db.InjuredPlayers.AsNoTracking().ToList();
        _byName = rows.ToDictionary(r => r.NormName, r => new Injury(r.PlayerName, r.TeamName, r.Status, r.Detail));
        UpdatedAt = rows.Count > 0 ? rows.Max(r => r.UpdatedAt) : null;
    }

    public void Failed(string error) => LastError = error;
}

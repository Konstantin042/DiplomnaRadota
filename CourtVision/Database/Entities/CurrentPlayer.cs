// Таблица CurrentPlayers: сегашните състави на отборите (сезон 2026-27, от NBA.com)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtVision.Database;

public class CurrentPlayer
{
    // Първичен ключ: ID-то на играча в NBA.com (по него се взима и снимката му)
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NbaId { get; set; }

    // Външен ключ към Teams.Abbr
    [Required, MaxLength(3)]
    public string TeamAbbr { get; set; } = "";
    public Team? Team { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = "";
    // Номерът на фланелката (текст, защото има и "00")
    [MaxLength(3)]
    public string Jersey { get; set; } = "";
    // Позицията, както е в NBA.com: G, F, C, G-F, F-C...
    [MaxLength(5)]
    public string Position { get; set; } = "";
    // Ръстът във футове и инчове, например "6-8"
    [MaxLength(5)]
    public string Height { get; set; } = "";
    public int Age { get; set; }

    // Снимката от сайта на NBA
    [NotMapped]
    public string PhotoUrl => $"https://cdn.nba.com/headshots/nba/latest/260x190/{NbaId}.png";

    // Ръстът в метри, например 6-8 → 2.03
    [NotMapped]
    public string HeightMeters
    {
        get
        {
            var parts = Height.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var feet) || !int.TryParse(parts[1], out var inches)) return "";
            return ((feet * 12 + inches) * 0.0254).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " m";
        }
    }
}

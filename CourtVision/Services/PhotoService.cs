// Генерира HTML за снимката на играч (кръгче със снимка или инициали)
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Html;

namespace CourtVision.Services;

// Услуга за снимките на играчите
public class PhotoService
{
    private readonly DataService _data;
    private readonly Dictionary<int, int> _nbaRows;
    private readonly Dictionary<int, int> _bbrefRows;

    public PhotoService(DataService data)
    {
        _data = data;
        _nbaRows = CountRows(data.NbaPhotos);
        _bbrefRows = CountRows(data.BbrefPhotos);
    }

    // Брои колко реда снимки има във всяко изображение
    private static Dictionary<int, int> CountRows(IReadOnlyDictionary<string, int[]> index) =>
        index.Values
            .GroupBy(v => v[0])
            .ToDictionary(g => g.Key, g => g.Max(v => v[1]) / 16 + 1);

    // Инициалите на играч, например "LJ" за LeBron James
    public static string Initials(string name) =>
        string.Concat(name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0]));

    // Връща HTML за снимката: име на играча, идентификатор и CSS цвят на рамката
    public IHtmlContent Face(string name, string? id, string colorVar = "PG")
    {
        id ??= _data.NameToId.GetValueOrDefault(DataService.NormalizeName(name));
        var style = $"--pc:var(--{colorVar})";
        var initials = WebUtility.HtmlEncode(Initials(name));
        var label = WebUtility.HtmlEncode(name);
        if ((id != null && _data.NbaPhotos.TryGetValue(id, out var nba)) || _data.NbaPhotos.TryGetValue("rk:" + name, out nba))
            return new HtmlString($"<div class=\"ph nba\" style=\"{style}\"><span>{initials}</span>{Sprite($"faces/n{nba[0]}.webp", nba[1], _nbaRows[nba[0]], label)}</div>");
        if (id != null && _data.BbrefPhotos.TryGetValue(id, out var bb))
            return new HtmlString($"<div class=\"ph\" style=\"{style}\"><span>{initials}</span>{Sprite($"faces/f{bb[0]}.jpg", bb[1], _bbrefRows[bb[0]], label)}</div>");
        return new HtmlString($"<div class=\"ph\" style=\"{style}\"><span>{initials}</span></div>");
    }

    // HTML за една снимка от голямо изображение (sprite sheet): 16 снимки на ред
    private static string Sprite(string file, int position, int rows, string label)
    {
        var col = position % 16;
        var row = position / 16;
        // Хоризонтална позиция в проценти (CSS background-position)
        var x = (col / 15.0 * 100).ToString("0.###", CultureInfo.InvariantCulture);
        var y = (rows > 1 ? row / (double)(rows - 1) * 100 : 0).ToString("0.###", CultureInfo.InvariantCulture);
        return $"<i class=\"face\" role=\"img\" aria-label=\"{label}\" style=\"background-image:url(/{file});background-size:1600% {rows * 100}%;background-position:{x}% {y}%\"></i>";
    }
}

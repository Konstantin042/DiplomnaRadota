// Генерира HTML за снимката на играч (кръгче със снимка или инициали)
using System.Globalization;               // За форматиране на числа с точка
using System.Net;                         // За HtmlEncode (безопасен текст в HTML)
using Microsoft.AspNetCore.Html;          // За HtmlString (готов HTML за Razor)

namespace CourtVision.Services; // Пространство от имена за услугите

// Услуга за снимките на играчите
public class PhotoService
{
    // Данните с индексите на снимките
    private readonly DataService _data;
    // Брой редове във всяко изображение с официални NBA снимки
    private readonly Dictionary<int, int> _nbaRows;
    // Брой редове във всяко изображение с резервни снимки
    private readonly Dictionary<int, int> _bbrefRows;

    // Конструктор: получава данните чрез внедряване на зависимости
    public PhotoService(DataService data)
    {
        // Запомняме данните
        _data = data;
        // Изчисляваме редовете на всяко изображение (по 16 снимки на ред)
        _nbaRows = CountRows(data.NbaPhotos);
        // Същото за резервните снимки
        _bbrefRows = CountRows(data.BbrefPhotos);
    }

    // Брои колко реда снимки има във всяко изображение
    private static Dictionary<int, int> CountRows(IReadOnlyDictionary<string, int[]> index) =>
        index.Values
            .GroupBy(v => v[0])                                            // Групираме по номер на изображението
            .ToDictionary(g => g.Key, g => g.Max(v => v[1]) / 16 + 1);     // Най-голямата позиция / 16 + 1 = брой редове

    // Инициалите на играч, например "LJ" за LeBron James
    public static string Initials(string name) =>
        string.Concat(name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0]));

    // Връща HTML за снимката: име на играча, идентификатор и CSS цвят на рамката
    public IHtmlContent Face(string name, string? id, string colorVar = "PG")
    {
        // Ако нямаме идентификатор, опитваме да го намерим по име
        id ??= _data.NameToId.GetValueOrDefault(DataService.NormalizeName(name));
        // CSS стил за цвета на рамката (променлива като --PG или --orange)
        var style = $"--pc:var(--{colorVar})";
        // Инициалите, безопасно кодирани за HTML
        var initials = WebUtility.HtmlEncode(Initials(name));
        // Името, безопасно кодирано за атрибут
        var label = WebUtility.HtmlEncode(name);
        // Първо търсим официална NBA снимка (по идентификатор или по "rk:Име" за новобранци)
        if ((id != null && _data.NbaPhotos.TryGetValue(id, out var nba)) || _data.NbaPhotos.TryGetValue("rk:" + name, out nba))
            // Връщаме кръгче с официалната снимка
            return new HtmlString($"<div class=\"ph nba\" style=\"{style}\"><span>{initials}</span>{Sprite($"faces/n{nba[0]}.webp", nba[1], _nbaRows[nba[0]], label)}</div>");
        // После търсим резервна снимка от Basketball-Reference
        if (id != null && _data.BbrefPhotos.TryGetValue(id, out var bb))
            // Връщаме кръгче с резервната снимка
            return new HtmlString($"<div class=\"ph\" style=\"{style}\"><span>{initials}</span>{Sprite($"faces/f{bb[0]}.jpg", bb[1], _bbrefRows[bb[0]], label)}</div>");
        // Ако няма снимка, показваме само инициалите
        return new HtmlString($"<div class=\"ph\" style=\"{style}\"><span>{initials}</span></div>");
    }

    // HTML за една снимка от голямо изображение (sprite sheet): 16 снимки на ред
    private static string Sprite(string file, int position, int rows, string label)
    {
        // Колона в изображението (0–15)
        var col = position % 16;
        // Ред в изображението
        var row = position / 16;
        // Хоризонтална позиция в проценти (CSS background-position)
        var x = (col / 15.0 * 100).ToString("0.###", CultureInfo.InvariantCulture);
        // Вертикална позиция в проценти (ако има само един ред, е 0)
        var y = (rows > 1 ? row / (double)(rows - 1) * 100 : 0).ToString("0.###", CultureInfo.InvariantCulture);
        // Връщаме елемента с фоново изображение, увеличено така, че да се вижда само една снимка
        return $"<i class=\"face\" role=\"img\" aria-label=\"{label}\" style=\"background-image:url(/{file});background-size:1600% {rows * 100}%;background-position:{x}% {y}%\"></i>";
    }
}

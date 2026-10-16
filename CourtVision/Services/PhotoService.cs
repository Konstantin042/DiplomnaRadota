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

    // Конструктор: получава данните чрез внедряване на зависимости
    public PhotoService(DataService data)
    {
        // Запомняме данните
        _data = data;
    }

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
        // Ако няма снимка, показваме само инициалите
        return new HtmlString($"<div class=\"ph\" style=\"{style}\"><span>{initials}</span></div>");
    }

}

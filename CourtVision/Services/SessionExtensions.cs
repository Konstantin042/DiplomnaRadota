// Помощни методи за запис и четене на обекти в сесията
using System.Text.Json; // Библиотеката за превръщане на обекти в JSON и обратно

namespace CourtVision.Services; // Пространство от имена за услугите

// Статичен клас с разширения за ISession
public static class SessionExtensions
{
    // Записва обект в сесията под даден ключ, като го превръща в JSON текст
    public static void SetObject<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    // Чете обект от сесията; ако го няма, създава нов с конструктора по подразбиране
    public static T GetObject<T>(this ISession session, string key) where T : new()
    {
        // Взимаме JSON текста от сесията
        var json = session.GetString(key);
        // Ако няма записан текст, връщаме нов празен обект
        if (string.IsNullOrEmpty(json)) return new T();
        // Иначе превръщаме текста обратно в обект (или нов, ако е повреден)
        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    // Чете обект, но връща null, ако няма такъв (за незадължителни данни)
    public static T? GetObjectOrNull<T>(this ISession session, string key) where T : class
    {
        // Взимаме JSON текста от сесията
        var json = session.GetString(key);
        // Връщаме null, ако няма текст, иначе обекта
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);
    }

    // Истина, ако заявката идва от нашия JavaScript (fetch), а не от обикновен формуляр
    public static bool IsAjax(this HttpRequest request) =>
        request.Headers["X-Requested-With"] == "fetch";
}

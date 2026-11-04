// Помощни методи за запис и четене на обекти в сесията
using System.Text.Json;

namespace CourtVision.Services;

// Статичен клас с разширения за ISession
public static class SessionExtensions
{
    // Записва обект в сесията под даден ключ, като го превръща в JSON текст
    public static void SetObject<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    // Чете обект от сесията; ако го няма, създава нов с конструктора по подразбиране
    public static T GetObject<T>(this ISession session, string key) where T : new()
    {
        var json = session.GetString(key);
        if (string.IsNullOrEmpty(json)) return new T();
        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    // Чете обект, но връща null, ако няма такъв (за незадължителни данни)
    public static T? GetObjectOrNull<T>(this ISession session, string key) where T : class
    {
        var json = session.GetString(key);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);
    }

    // Истина, ако заявката идва от нашия JavaScript (fetch), а не от обикновен формуляр
    public static bool IsAjax(this HttpRequest request) =>
        request.Headers["X-Requested-With"] == "fetch";
}

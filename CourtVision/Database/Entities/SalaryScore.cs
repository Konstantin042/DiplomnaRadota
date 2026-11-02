// Таблица SalaryScores: подписаните билдове в Salary Cap Game (класацията за всеки ден)
using System.ComponentModel.DataAnnotations; // Атрибути за ключове и дължини на колоните
using Microsoft.AspNetCore.Identity;         // IdentityUser – потребителят от таблицата AspNetUsers

namespace CourtVision.Database; // Пространство от имена за базата данни

// Един ред от таблицата SalaryScores
public class SalaryScore
{
    // Първичен ключ (номерира се автоматично от базата)
    public int Id { get; set; }
    // Денят на мрежата, например "2026-11-02", или "inf" за безкрайната игра
    [Required, MaxLength(10)]
    public string DayKey { get; set; } = "";
    // Идентификатор на сесията (за да покажем "You" на собствения ред)
    [Required, MaxLength(100)]
    public string Owner { get; set; } = "";
    // Външен ключ към AspNetUsers (NULL за гост)
    [MaxLength(450)]
    public string? UserId { get; set; }
    // Навигация към потребителя
    public IdentityUser? User { get; set; }
    // Името в класацията
    [Required, MaxLength(60)]
    public string PlayerName { get; set; } = "";
    // Петимата подписани играчи: номерата им, разделени със запетая, например "12,45,301,7,88"
    [Required, MaxLength(60)]
    public string Players { get; set; } = "";
    // Обща оценка на билда (OVR)
    public int Overall { get; set; }
    // PPG + APG + RPG (за подреждане при равна оценка)
    public double Total { get; set; }
    // Неизхарчени милиони под тавана
    public int Spare { get; set; }
    // Кога е подписан билдът (по UTC)
    public DateTime CreatedAt { get; set; }
}

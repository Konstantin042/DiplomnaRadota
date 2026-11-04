// Таблица SalaryScores: подписаните билдове в Salary Cap Game (класацията за всеки ден)
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CourtVision.Database;

// Един ред от таблицата SalaryScores
public class SalaryScore
{
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
    public IdentityUser? User { get; set; }
    // Името в класацията
    [Required, MaxLength(60)]
    public string PlayerName { get; set; } = "";
    // Петимата подписани играчи: номерата им, разделени със запетая, например "12,45,301,7,88"
    [Required, MaxLength(60)]
    public string Players { get; set; } = "";
    public int Overall { get; set; }
    public double Total { get; set; }
    public int Spare { get; set; }
    public DateTime CreatedAt { get; set; }
}

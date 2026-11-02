// Моделите на формулярите за регистрация и вход и данните за страницата Profile
using System.ComponentModel.DataAnnotations; // Атрибути за проверка на полетата (Required, StringLength и др.)
using CourtVision.Database;                  // GameResult – редовете от таблицата с резултатите
using CourtVision.Services;                  // LeaderRow – най-добрият резултат в игра

namespace CourtVision.Models; // Пространство от имена за моделите

// Формулярът за регистрация
public class RegisterModel
{
    // Потребителско име: от 3 до 20 знака, само латински букви, цифри, точка, тире и долна черта
    [Required(ErrorMessage = "Choose a username.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "The username must be 3 to 20 characters long.")]
    [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "Use only letters, numbers, dots, dashes and underscores.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    // Паролата: поне 6 знака (полето се показва със звездички)
    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    // Повторение на паролата: трябва да съвпада с Password
    [Required(ErrorMessage = "Repeat the password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repeat password")]
    public string ConfirmPassword { get; set; } = "";

    // Къде да се върнем след регистрацията (например в играта, от която е дошъл)
    public string? ReturnUrl { get; set; }
}

// Формулярът за вход
public class LoginModel
{
    // Потребителското име
    [Required(ErrorMessage = "Enter your username.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    // Паролата (полето се показва със звездички)
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    // Да се помни ли входът след затваряне на браузъра
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    // Къде да се върнем след входа
    public string? ReturnUrl { get; set; }
}

// Данните за страницата Profile
public class ProfileModel
{
    // Потребителското име
    public string UserName { get; set; } = "";
    // Най-добрият резултат и броят игри във всяка игра (ключ на играта → ред)
    public Dictionary<string, LeaderRow> Bests { get; set; } = new();
    // Мястото в класацията на всяка игра (ключ на играта → място или null)
    public Dictionary<string, int?> Ranks { get; set; } = new();
    // Последните изиграни игри
    public List<GameResult> Recent { get; set; } = new();
    // Общо изиграни игри
    public int TotalPlayed => Bests.Values.Sum(b => b.Played);
}

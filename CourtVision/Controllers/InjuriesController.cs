// GET /Injuries/Json – текущият доклад за контузиите (страницата на отбора го проверява всяка минута)
using CourtVision.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourtVision.Controllers;

public class InjuriesController : Controller
{
    private readonly InjuryService _injuries;

    public InjuriesController(InjuryService injuries) => _injuries = injuries;

    public IActionResult Json() => Json(new
    {
        updated = _injuries.UpdatedAt,
        players = _injuries.All.Select(i => new { name = InjuryService.Normalize(i.Name), status = i.Status, detail = i.Detail }),
    });
}

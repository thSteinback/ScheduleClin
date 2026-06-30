using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScheduleClin.Models;

namespace ScheduleClin.Controllers;

[Authorize(Roles = Perfis.Paciente)]
public class PacienteController : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Consultas));

    public IActionResult Consultas() => View();

    public IActionResult Historico() => View();
}

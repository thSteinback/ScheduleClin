using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleClin.Context;
using ScheduleClin.DTO;
using ScheduleClin.Models;

namespace ScheduleClin.Controllers;

// API somente-leitura da agenda do psicólogo logado.
// Retorna apenas os itens sob responsabilidade dele (PsicologoId == usuário atual),
// incluindo bloqueios/eventos sem paciente associado.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Psicologo)]
public class PsicologoAgendaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public PsicologoAgendaController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarDto>>> GetMinhaAgenda(DateTime? inicio, DateTime? fim)
    {
        var psicologoId = Guid.Parse(_userManager.GetUserId(User)!);

        var query = _context.Calendars
            .Where(c => c.PsicologoId == psicologoId);

        if (inicio.HasValue)
            query = query.Where(c => c.ScheduleDate >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(c => c.ScheduleDate <= fim.Value);

        var consultas = await query.OrderBy(c => c.ScheduleDate).ToListAsync();

        var pacienteIds = consultas
            .Where(c => c.PacienteId.HasValue)
            .Select(c => c.PacienteId!.Value)
            .Distinct()
            .ToList();

        var pacientes = await _context.Users
            .Where(u => pacienteIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        var nomePsicologo = (await _userManager.GetUserAsync(User))?.UserName;

        var dtos = consultas.Select(c => new CalendarDto
        {
            CalendarId      = c.CalendarID,
            Title           = c.Title,
            ScheduleDate    = c.ScheduleDate,
            DurationMinutes = c.DurationMinutes,
            Status          = c.Status,
            PacienteId      = c.PacienteId,
            PacienteNome    = c.PacienteId.HasValue ? pacientes.GetValueOrDefault(c.PacienteId.Value) : null,
            PsicologoId     = c.PsicologoId,
            PsicologoNome   = nomePsicologo,
        });

        return Ok(dtos);
    }
}

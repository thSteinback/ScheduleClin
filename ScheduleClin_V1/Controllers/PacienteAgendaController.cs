using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleClin.Context;
using ScheduleClin.DTO;
using ScheduleClin.Models;

namespace ScheduleClin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Paciente)]
public class PacienteAgendaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public PacienteAgendaController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarDto>>> GetMinhasConsultas()
    {
        var pacienteId = Guid.Parse(_userManager.GetUserId(User)!);

        var consultas = await _context.Calendars
            .Where(c => c.PacienteId == pacienteId)
            .OrderBy(c => c.ScheduleDate)
            .ToListAsync();

        return Ok(await MapearDtos(consultas));
    }

    [HttpGet("historico")]
    public async Task<ActionResult<IEnumerable<CalendarDto>>> GetHistorico()
    {
        var pacienteId = Guid.Parse(_userManager.GetUserId(User)!);
        var agora = DateTime.Now;

        var consultas = await _context.Calendars
            .Where(c => c.PacienteId == pacienteId)
            .Where(c => c.ScheduleDate < agora || c.Status == AppointmentStatus.Cancelada || c.Status == AppointmentStatus.Finalizado)
            .OrderByDescending(c => c.ScheduleDate)
            .ToListAsync();

        return Ok(await MapearDtos(consultas));
    }

    [HttpGet("psicologos")]
    public async Task<ActionResult> GetPsicologos()
    {
        var psicologos = await _context.Users
            .Include(u => u.Perfil)
            .Where(u => u.Active && u.Perfil != null && u.Perfil.Name == Perfis.Psicologo)
            .Select(u => new { id = u.Id, nome = u.UserName })
            .ToListAsync();

        return Ok(psicologos);
    }

    private static readonly TimeSpan HoraAbertura   = TimeSpan.FromHours(7);
    private static readonly TimeSpan HoraFechamento = TimeSpan.FromHours(19);

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CalendarCreateDto dto)
    {
        var pacienteId = Guid.Parse(_userManager.GetUserId(User)!);

        if (!dto.PsicologoId.HasValue)
            return BadRequest(new { message = "Psicólogo é obrigatório." });

        if (dto.ScheduleDate.Date < DateTime.Today)
            return BadRequest(new { message = "Não é possível agendar em datas anteriores." });

        var hora = dto.ScheduleDate.TimeOfDay;
        if (hora < HoraAbertura || hora >= HoraFechamento)
            return BadRequest(new { message = "Só é possível agendar entre 07:00 e 19:00." });

        var conflito = await _context.Calendars.AnyAsync(c =>
            c.PsicologoId == dto.PsicologoId &&
            c.ScheduleDate == dto.ScheduleDate &&
            c.Status != AppointmentStatus.Cancelada &&
            c.Status != AppointmentStatus.Finalizado);

        if (conflito)
            return BadRequest(new { message = "Esse psicólogo já tem uma consulta nesse dia e horário." });

        var paciente = await _userManager.GetUserAsync(User);

        var consulta = new Calendar
        {
            CalendarID      = Guid.NewGuid(),
            Title           = dto.Title ?? $"Consulta – {paciente?.UserName}",
            ScheduleDate    = dto.ScheduleDate,
            DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 60,
            PacienteId      = pacienteId,
            PsicologoId     = dto.PsicologoId,
            Status          = AppointmentStatus.Pendente,
            CriadoPorId     = pacienteId,
        };

        _context.Calendars.Add(consulta);
        await _context.SaveChangesAsync();

        return Created($"/api/PacienteAgenda/{consulta.CalendarID}", new { id = consulta.CalendarID });
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var pacienteId = Guid.Parse(_userManager.GetUserId(User)!);

        var consulta = await _context.Calendars
            .FirstOrDefaultAsync(c => c.CalendarID == id && c.PacienteId == pacienteId);

        if (consulta is null)
            return NotFound(new { message = "Consulta não encontrada." });

        if (consulta.Status == AppointmentStatus.Cancelada)
            return BadRequest(new { message = "Esta consulta já está cancelada." });

        if (consulta.Status == AppointmentStatus.Finalizado)
            return BadRequest(new { message = "Não é possível cancelar uma consulta já finalizada." });

        consulta.Status = AppointmentStatus.Cancelada;
        await _context.SaveChangesAsync();

        return Ok(new { id = consulta.CalendarID, status = consulta.Status });
    }

    private async Task<IEnumerable<CalendarDto>> MapearDtos(List<Calendar> consultas)
    {
        var psicologoIds = consultas
            .Where(c => c.PsicologoId.HasValue)
            .Select(c => c.PsicologoId!.Value)
            .Distinct()
            .ToList();

        var psicologos = await _context.Users
            .Where(u => psicologoIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        return consultas.Select(c => new CalendarDto
        {
            CalendarId      = c.CalendarID,
            Title           = c.Title,
            ScheduleDate    = c.ScheduleDate,
            DurationMinutes = c.DurationMinutes,
            Status          = c.Status,
            PacienteId      = c.PacienteId,
            PsicologoId     = c.PsicologoId,
            PsicologoNome   = c.PsicologoId.HasValue ? psicologos.GetValueOrDefault(c.PsicologoId.Value) : null,
        });
    }
}

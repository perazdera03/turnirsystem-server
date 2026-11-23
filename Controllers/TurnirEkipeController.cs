using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za prijavu ekipa na turnir. radi sa veznom tabelom TurnirEkipa
[Authorize] //po defaultu je samo prijavljen admin, a javna metoda ima [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] //ime klase bez Controller = /api/TurnirEkipe
public class TurnirEkipeController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public TurnirEkipeController(ApplicationDbContext context)
    {
        _context = context;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class TurnirEkipaDto
    {
        public int TurnirId { get; set; }
        public int EkipaId { get; set; }
    }

    //get citaj metoda. ekipe prijavljene na jedan turnir
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}")]
    public async Task<IActionResult> GetByTurnir(int turnirId)
    {
        var ekipe = await _context.TurnirEkipe
            .Where(te => te.TurnirId == turnirId)
            .Include(te => te.Ekipa) //include ucitava i ekipu da bismo imali njen naziv
            .Select(te => new { te.TurnirEkipaId, te.EkipaId, te.Ekipa.Naziv })
            .ToListAsync();
        return Ok(ekipe);
    }

    //post (salji) metoda. prijava ekipe na turnir
    [HttpPost]
    public async Task<IActionResult> Prijavi([FromBody] TurnirEkipaDto dto)
    {
        //greska ako turnir ne postoji
        var turnir = await _context.Turniri.FindAsync(dto.TurnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        //ako su grupe generisane, ne moze vise da se prijave ekipe
        var grupeGenerisane = await _context.Grupe.AnyAsync(g => g.TurnirId == dto.TurnirId);
        if (grupeGenerisane)
            return BadRequest(new { message = "Grupe su već generisane, ekipe se više ne mogu prijavljivati." });

        //ekipa mora da postoji
        var ekipaPostoji = await _context.Ekipe.AnyAsync(e => e.EkipaId == dto.EkipaId);
        if (!ekipaPostoji)
            return NotFound(new { message = "Ekipa nije pronađena." });

        //ne moze ekipa biti prijavljena dva puta na isti turnir 
        var exists = await _context.TurnirEkipe
            .AnyAsync(te => te.TurnirId == dto.TurnirId && te.EkipaId == dto.EkipaId);
        if (exists)
            return Conflict(new { message = "Ekipa je već prijavljena na ovaj turnir." });

        //najvise ekipa moze biti po broju grupa * kapacitet tih grupa
        var maxEkipa = turnir.BrojGrupa * turnir.BrojEkipaPoGrupi;
        var brojPrijavljenih = await _context.TurnirEkipe.CountAsync(te => te.TurnirId == dto.TurnirId);
        if (brojPrijavljenih >= maxEkipa)
            return BadRequest(new { message = $"Maksimalan broj ekipa ({maxEkipa}) je već dostignut." });

        //ako svi uslovi prodju, pravi se prijava ekipe na turnir
        var turnirEkipa = new TurnirEkipa
        {
            TurnirId = dto.TurnirId,
            EkipaId = dto.EkipaId
        };
        _context.TurnirEkipe.Add(turnirEkipa);
        await _context.SaveChangesAsync();
        return Created("", new { turnirEkipa.TurnirEkipaId, turnirEkipa.TurnirId, turnirEkipa.EkipaId });
    }

    //delete metoda za brisanje. odjava ekipe sa turnira
    [HttpDelete("{id}")]
    public async Task<IActionResult> Ukloni(int id)
    {
        var turnirEkipa = await _context.TurnirEkipe.FindAsync(id);
        if (turnirEkipa == null) return NotFound(new { message = "Prijava nije pronađena." });

        //ako su grupe generisane, ekipa se ne moze vise odjaviti
        var grupeGenerisane = await _context.Grupe.AnyAsync(g => g.TurnirId == turnirEkipa.TurnirId);
        if (grupeGenerisane)
            return BadRequest(new { message = "Grupe su već generisane, ekipe se više ne mogu odjavljivati." });

        _context.TurnirEkipe.Remove(turnirEkipa);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za ekipe. prikazuje ekipe, pravi novi ekipe, izmenjuje i brise ekipe.
[Authorize] //po defaultu je samo prijavljen admin, a javne metode imaju [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] //ime klase bez Controller = /api/Ekipe
public class EkipeController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public EkipeController(ApplicationDbContext context)
    {
        _context = context;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class EkipaDto //informacija o ekipi
    {
        public string? Naziv { get; set; } //ime ekipe. moze biti prazno(null)
        public string? SkraceniNaziv { get; set; } //skraceni naziv ekipe. moze biti prazno(null)
        public string? Mesto { get; set; } //poreklo ekipe. moze biti prazno(null)
        public string? KontaktOsoba { get; set; } //ime menadzera ekipe. moze biti prazno(null)
        public string? LogoUrl { get; set; } //logo ekipe. ako ekipa zeli sliku, mora dati url slike. moze biti prazno(null)
    }

    //proverava adresu logoa. vraca poruku greske ili null ako je sve u redu
    private static string? ValidirajLogo(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null; //logo nije obavezan, prazno je ok
        url = url.Trim();
        //uslov da adresa nije predugacka
        if (url.Length > 500)
            return "Adresa loga je predugačka (najviše 500 karaktera)."; //ne moze iznad 500 karaktera
        //uslov da je adresa prava http ili https adresa
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return "Logo mora biti ispravna http ili https adresa slike.";
        return null;
    }

    //get citaj metoda, vraca sve ekipe
    [AllowAnonymous]  //svi mogu videti, ali ne mogu menjati
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ekipe = await _context.Ekipe
            .Select(e => new { e.EkipaId, e.Naziv, e.SkraceniNaziv, e.Mesto, e.KontaktOsoba, e.LogoUrl }) //uzimaju se samo potrebna polja
            .ToListAsync();
        return Ok(ekipe);
    }

    //get citaj metoda, vraca jednu ekipu na osnovu id
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ekipa = await _context.Ekipe
            .Where(e => e.EkipaId == id)
            .Select(e => new { e.EkipaId, e.Naziv, e.SkraceniNaziv, e.Mesto, e.KontaktOsoba, e.LogoUrl })
            .FirstOrDefaultAsync();
        if (ekipa == null) return NotFound(new { message = "Ekipa nije pronađena." }); //ekipa ne postoji, daje gresku 404
        return Ok(ekipa);
    }

    //post (salji) metoda. pravi novu ekipu
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EkipaDto dto)
    {
        //naziv je jedino obavezno polje
        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return BadRequest(new { message = "Naziv ekipe je obavezan." });

        var greskaLogo = ValidirajLogo(dto.LogoUrl); //provera loga
        if (greskaLogo != null) return BadRequest(new { message = greskaLogo });

        //pravimo novu ekipu od podataka koji salje klijent
        var ekipa = new Ekipa
        {
            Naziv = dto.Naziv.Trim(),
            SkraceniNaziv = dto.SkraceniNaziv,
            Mesto = dto.Mesto,
            KontaktOsoba = dto.KontaktOsoba,
            LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim()
        };
        _context.Ekipe.Add(ekipa);
        await _context.SaveChangesAsync(); //tek ovde se upisuje u bazu
        return Created("", new { ekipa.EkipaId, ekipa.Naziv });
    }

    //put http metoda za editovanje sadrzaja. izmena ekipe
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] EkipaDto dto)
    {
        var ekipa = await _context.Ekipe.FindAsync(id); //trazimo ekipu po id-u
        if (ekipa == null) return NotFound(new { message = "Ekipa nije pronađena." });

        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return BadRequest(new { message = "Naziv ekipe je obavezan." });

        var greskaLogo = ValidirajLogo(dto.LogoUrl);
        if (greskaLogo != null) return BadRequest(new { message = greskaLogo });

        //menjamo polja koja je klijent poslao
        ekipa.Naziv = dto.Naziv.Trim();
        ekipa.SkraceniNaziv = dto.SkraceniNaziv;
        ekipa.Mesto = dto.Mesto;
        ekipa.KontaktOsoba = dto.KontaktOsoba;
        ekipa.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();

        await _context.SaveChangesAsync();
        return NoContent(); //uspeh bez odgovora. poruka 204
    }

    //delete metoda za brisanje. brisanje ekipe ima uslove
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ekipa = await _context.Ekipe.FindAsync(id);
        if (ekipa == null) return NotFound(new { message = "Ekipa nije pronađena." });

        //ne moze da se brise ekipa dok se ne zavrsi turnir
        var prijavljena = await _context.TurnirEkipe.AnyAsync(te => te.EkipaId == id);
        if (prijavljena)
            return Conflict(new { message = "Ekipa je prijavljena na turnir. Prvo je odjavi sa turnira (ili obriši turnir)." });

        //isto ako je igrao bilo koji mec, da ne ostane istorija bez ekipe
        var uMecevima = await _context.Mecevi.AnyAsync(m => m.Ekipa1Id == id || m.Ekipa2Id == id)
            || await _context.EliminacionaKola.AnyAsync(k => k.Ekipa1Id == id || k.Ekipa2Id == id || k.PobednikId == id);
        if (uMecevima)
            return Conflict(new { message = "Ekipa učestvuje u mečevima i ne može se obrisati." });

        //ako svi uslovi prolaze, brise se ekipa
        _context.Ekipe.Remove(ekipa);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
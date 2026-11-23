using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za turnire. izvrsava pravljenje, izmena, brisanje i prikaz turnira 
[Authorize] //po defaultu je samo prijavljen admin, a javne metode imaju [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] //ime klase bez Controller = /api/Turniri
public class TurniriController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public TurniriController(ApplicationDbContext context)
    {
        _context = context;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class TurnirDto
    {
        public string? Naziv { get; set; } //naziv turnira. obavezno polje zbog provere Validiraj funkcije
        public string? Opis { get; set; } //opis turnira. moze biti prazno(null)
        public DateTime DatumPocetka { get; set; } //pocetak turnira. obavezno polje
        public DateTime? DatumZavrsetka { get; set; } //kad se zavrsava turnir. moze biti prazno(null)
        public int BrojGrupa { get; set; } //broj grupa
        public int BrojEkipaPoGrupi { get; set; } //koliko ekipa moze stati u grupi
        public int BrojKvalifikovanih { get; set; } //koliko kvalifikovanih po grupi
        public int BodPobeda { get; set; } = 3; //bodovi za pobedu
        public int BodPoraz { get; set; } = 0; //bodovi za poraz
    }

    //provera podataka. vratice poruku greske, ili null ako je u redu
    private static string? Validiraj(TurnirDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return "Naziv turnira je obavezan."; //turnir mora imati ime
        if (dto.BrojGrupa < 1 || dto.BrojGrupa > 26) //moze postojati najvise 26 grupa. to je toliko slova
            return "Broj grupa mora biti između 1 i 26.";
        if (dto.BrojEkipaPoGrupi < 2) //mora imati bar dve ekipe
            return "Svaka grupa mora imati bar 2 ekipe.";
        if (dto.BrojKvalifikovanih < 1 || dto.BrojKvalifikovanih > dto.BrojEkipaPoGrupi) //ne moze imati vise kvalifikovanih nego broj ekipa u grupi
            return "Broj kvalifikovanih po grupi mora biti između 1 i broja ekipa po grupi.";
        if (dto.BrojGrupa * dto.BrojKvalifikovanih < 2) //mora imati makar 2 ekipe u elim.fazi
            return "Iz grupne faze moraju da prođu bar 2 ekipe.";
        if (dto.BodPobeda < 0 || dto.BodPoraz < 0)
            return "Bodovi ne mogu biti negativni."; //bodovi ne mogu biti negativni
        if (dto.DatumZavrsetka.HasValue && dto.DatumZavrsetka.Value < dto.DatumPocetka) //datum zavrsetka ne moze biti pre pocetka
            return "Datum završetka ne može biti pre datuma početka.";
        return null;
    }

    //get citaj metoda. lista svih turnira.
    [AllowAnonymous] //svi mogu videti
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var turniri = await _context.Turniri
            .Select(t => new { t.TurnirId, t.Naziv, t.Opis, t.DatumPocetka, t.DatumZavrsetka, t.Status, t.BrojGrupa, t.BrojEkipaPoGrupi }) //za listu saljemo samo osnovna polja
            .ToListAsync();
        return Ok(turniri);
    }

    //get citaj metoda. jedan turnir sa svim podesavanjima
    [AllowAnonymous] //svi mogu videti
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var turnir = await _context.Turniri
            .Where(t => t.TurnirId == id)
            .Select(t => new {
                t.TurnirId,
                t.Naziv,
                t.Opis,
                t.DatumPocetka,
                t.DatumZavrsetka,
                t.BrojGrupa,
                t.BrojEkipaPoGrupi,
                t.BrojKvalifikovanih,
                t.BodPobeda,
                t.BodPoraz,
                t.Status,
                t.GrupnaFazaZakljucana
            })
            .FirstOrDefaultAsync();
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //daje gresku 404 ako turnir ne postoji
        return Ok(turnir);
    }

    //post (salji) metoda. pravi novi turnir
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TurnirDto dto)
    {
        var greska = Validiraj(dto);
        if (greska != null) return BadRequest(new { message = greska }); //koristi se funkcija validiraj, daje poruku ako ima greske

        var turnir = new Turnir
        {
            Naziv = dto.Naziv!.Trim(),
            Opis = dto.Opis,
            DatumPocetka = DateTime.SpecifyKind(dto.DatumPocetka, DateTimeKind.Utc), //datum cuvamo kao UTC da se ne pomeri
            DatumZavrsetka = dto.DatumZavrsetka.HasValue ? DateTime.SpecifyKind(dto.DatumZavrsetka.Value, DateTimeKind.Utc) : null,
            BrojGrupa = dto.BrojGrupa,
            BrojEkipaPoGrupi = dto.BrojEkipaPoGrupi,
            BrojKvalifikovanih = dto.BrojKvalifikovanih,
            BodPobeda = dto.BodPobeda,
            BodPoraz = dto.BodPoraz,
            Status = TurnirStatus.Priprema //turnir uvek pocinje od pripreme
        };
        _context.Turniri.Add(turnir);
        await _context.SaveChangesAsync();
        return Created("", new { turnir.TurnirId, turnir.Naziv, turnir.Status }); //uspesno pravi i vraca id, naziv i status turnira
    }

    //put http metoda za editovanje sadrzaja. izmena turnira
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TurnirDto dto)
    {
        var turnir = await _context.Turniri.FindAsync(id);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //ne moze da se menja turnir ako ne postoji

        var greska = Validiraj(dto);
        if (greska != null) return BadRequest(new { message = greska }); //isto se koristi funkcija validiraj. daje poruku usled greske

        //podesavanje turnira (broj grupa, broj ekipa, kvalifikovanih, bodovi)
        var grupeGenerisane = await _context.Grupe.AnyAsync(g => g.TurnirId == id);
        bool strukturaPromenjena =
            dto.BrojGrupa != turnir.BrojGrupa ||
            dto.BrojEkipaPoGrupi != turnir.BrojEkipaPoGrupi ||
            dto.BrojKvalifikovanih != turnir.BrojKvalifikovanih ||
            dto.BodPobeda != turnir.BodPobeda ||
            dto.BodPoraz != turnir.BodPoraz;

        if (grupeGenerisane && strukturaPromenjena)
            return BadRequest(new { message = "Grupe su već generisane, struktura i bodovanje turnira se više ne mogu menjati." }); //ako su grupe vec generisane, podesavanje turnira se ne moze vise menjati

        var prijavljeno = await _context.TurnirEkipe.CountAsync(te => te.TurnirId == id);
        if (dto.BrojGrupa * dto.BrojEkipaPoGrupi < prijavljeno)
            return BadRequest(new { message = $"Na turnir je već prijavljeno {prijavljeno} ekipa, a nova struktura dozvoljava {dto.BrojGrupa * dto.BrojEkipaPoGrupi}. Prvo odjavi višak ekipa." }); //ne moze da se menja kapacitet ekipa ako je vec prijavljeno vise od kapaciteta

        turnir.Naziv = dto.Naziv!.Trim();
        turnir.Opis = dto.Opis;
        turnir.DatumPocetka = DateTime.SpecifyKind(dto.DatumPocetka, DateTimeKind.Utc);
        turnir.DatumZavrsetka = dto.DatumZavrsetka.HasValue
            ? DateTime.SpecifyKind(dto.DatumZavrsetka.Value, DateTimeKind.Utc)
            : null;
        turnir.BrojGrupa = dto.BrojGrupa;
        turnir.BrojEkipaPoGrupi = dto.BrojEkipaPoGrupi;
        turnir.BrojKvalifikovanih = dto.BrojKvalifikovanih;
        turnir.BodPobeda = dto.BodPobeda;
        turnir.BodPoraz = dto.BodPoraz;

        await _context.SaveChangesAsync();
        return NoContent(); //ako je sve u redu, menjaju se polja bez poruke
    }

    //delete metoda za brisanje. brise turnir
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var turnir = await _context.Turniri.FindAsync(id);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //gresku daje ako pokusamo da brisemo turnir koji ne postoji

        var eliminacija = await _context.EliminacionaKola.Where(e => e.TurnirId == id).ToListAsync();
        _context.EliminacionaKola.RemoveRange(eliminacija);

        var mecevi = await _context.Mecevi.Where(m => m.TurnirId == id).ToListAsync();
        _context.Mecevi.RemoveRange(mecevi);

        var grupe = await _context.Grupe.Where(g => g.TurnirId == id).ToListAsync();
        foreach (var grupa in grupe)
        {
            var grupaEkipe = await _context.GrupaEkipe.Where(ge => ge.GrupaId == grupa.GrupaId).ToListAsync();
            _context.GrupaEkipe.RemoveRange(grupaEkipe);
        }
        _context.Grupe.RemoveRange(grupe);

        var turnirEkipe = await _context.TurnirEkipe.Where(te => te.TurnirId == id).ToListAsync();
        _context.TurnirEkipe.RemoveRange(turnirEkipe);

        _context.Turniri.Remove(turnir);
        await _context.SaveChangesAsync();
        return NoContent(); //brise se redom:eliminacija, mecevi, ekipe u grupama, grupe, prijave i na kraju turnir zbog veza tabela
    }
}
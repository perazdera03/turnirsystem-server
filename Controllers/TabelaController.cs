using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Services;

//kontroler za tabelu grupe. postavlja redosled tabele i prikazuje klijentu. racunanje u tabelu izvrsava servis TabelaRacun
[ApiController] //nema [Authorize] na klasi, jer je tabela javna. jedino metoda za rucni redosled trazi prijavu
[Route("api/[controller]")] //ime klase bez Controller = /api/Tabela
public class TabelaController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public TabelaController(ApplicationDbContext context)
    {
        _context = context;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class RedosledDto
    {
        public List<int> EkipaIds { get; set; } = new(); //id ekipe po redosledu. admin bira, a prva ekipa u spisku je najbolja ekipa
    }

    //get citaj metoda. tabela jedne grupe. svako moze videti
    [HttpGet("{grupaId}")]
    public async Task<IActionResult> GetTabela(int grupaId)
    {
        //ucitava se grupa sa ekipama i mecevima. obavezno zbog racunanja
        var grupa = await _context.Grupe
            .Include(g => g.GrupaEkipe).ThenInclude(ge => ge.Ekipa)
            .Include(g => g.Mecevi)
            .FirstOrDefaultAsync(g => g.GrupaId == grupaId);

        if (grupa == null) return NotFound(new { message = "Grupa nije pronađena." }); //poruka za gresku ako ne postoji grupa

        var turnir = await _context.Turniri.FindAsync(grupa.TurnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //poruka za gresku ako ne postoji turnir

        //funkcija iz servisa tabelaracun. racuna se tabela grupe iz meceva svaki put kada se ucita stranica. ne cuva se u bazi
        var tabela = TabelaRacun.Izracunaj(grupa, turnir)
            .Select(r => new
            {
                r.EkipaId,
                r.Naziv,
                r.OU,
                r.P,
                r.I,
                r.Poeni,
                r.PrimljeniPoeni,
                r.Razlika,
                r.Bod,
                r.RucniPlasman,
                r.Izjednacenje,
                r.RucnoOdredjen
            })
            .ToList();

        return Ok(tabela);
    }

    //put http metoda za editovanje sadrzaja. rucno postavljanje ekipa u grupi koje su izjednacene
    [Authorize] //samo admin moze da postavlja redosled
    [HttpPut("{grupaId}/redosled")]
    public async Task<IActionResult> PostaviRedosled(int grupaId, [FromBody] RedosledDto dto)
    {
        //ucitava se grupa sa ekipama i mecevima. obavezno zbog racunanja
        var grupa = await _context.Grupe
            .Include(g => g.GrupaEkipe).ThenInclude(ge => ge.Ekipa)
            .Include(g => g.Mecevi)
            .FirstOrDefaultAsync(g => g.GrupaId == grupaId);

        if (grupa == null) return NotFound(new { message = "Grupa nije pronađena." }); //greska ako grupa ne postoji

        var turnir = await _context.Turniri.FindAsync(grupa.TurnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako turnir ne postoji

        //ako je grupna faza zakljucana, redosled je sacuvan i ne moze se vise menjati
        if (turnir.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Grupna faza je zaključana, redosled se više ne može menjati." });

        //ako je eliminaciona faza generisana, redosled je sacuvan i ne moze se vise menjati
        var postojiEliminacija = await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == grupa.TurnirId);
        if (postojiEliminacija)
            return BadRequest(new { message = "Eliminaciona faza je već generisana, redosled se više ne može menjati." });

        //uslov, mora imati minimum 2 razlicite ekipe
        if (dto.EkipaIds == null || dto.EkipaIds.Count < 2 || dto.EkipaIds.Distinct().Count() != dto.EkipaIds.Count)
            return BadRequest(new { message = "Potrebne su bar 2 različite ekipe." });

        var tabela = TabelaRacun.Izracunaj(grupa, turnir); //funkcija servisa tabelaracun, racuna tabelu grupe
        var izabrani = tabela.Where(r => dto.EkipaIds.Contains(r.EkipaId)).ToList(); //prikazuje se lista ekipa koja je admin izabrao

        //izabrane ekipe moraju biti isto izjednacene. redosled se ne moze postaviti za ekipe koje nisu isto izjednacene
        if (izabrani.Count != dto.EkipaIds.Count
            || izabrani.Any(r => r.Izjednacenje == null)
            || izabrani.Select(r => r.Izjednacenje).Distinct().Count() != 1)
            return BadRequest(new { message = "Redosled se može određivati samo za ekipe koje su potpuno izjednačene." });

        //postavljanje ekipa u plasmanu od najboljeg do najgoreg
        for (int i = 0; i < dto.EkipaIds.Count; i++)
        {
            var clan = grupa.GrupaEkipe.First(ge => ge.EkipaId == dto.EkipaIds[i]);
            clan.RucniPlasman = i + 1;
        }

        await _context.SaveChangesAsync(); //kad se sve zavrsi, upisuje se u bazu
        return Ok(new { message = "Redosled sačuvan." });
    }
}

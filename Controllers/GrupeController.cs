using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;
using TurnirSystem.Services;

// kontroler za grupe. ovim kontrolerom se prave grupe rucno ili nasumicno, moguce zamene ekipa i zakljucavanje grupne faze
[Authorize] // po defaultu je samo prijavljen admin, a javna metoda ima [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] // ime klase bez "Controller" = /api/Grupe
public class GrupeController : ControllerBase
{
    // oznake grupa grupne faze po redu, ide od A do Z
    private const string Oznake = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly ApplicationDbContext _context; // pristup bazi

    public GrupeController(ApplicationDbContext context)
    {
        _context = context;
    }


    public class RasporedStavka //jedna stavka rucnog rasporeda: koja ekipa ide gde
    {
        public int EkipaId { get; set; }
        public string? Grupa { get; set; }
    }

    // DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class RasporedDto //ceo rucni raspored, tj cela lista stavki
    {
        public List<RasporedStavka> Raspored { get; set; } = new();
    }

    // dve ekipe koje se zamenjuju
    public class ZamenaDto
    {
        public int Ekipa1Id { get; set; }
        public int Ekipa2Id { get; set; }
    }

    private static (int Min, int Max, int MinPoGrupi) Granice(Turnir t)  //minimum i maximum ekipa koje turnir moze imati za formiranje grupa. 
    {
        int minPoGrupi = Math.Max(2, t.BrojKvalifikovanih); //mora da ima dve ekipe zbog meca, ali i onoliko ekipa da prolaze dalje
        return (t.BrojGrupa * minPoGrupi, t.BrojGrupa * t.BrojEkipaPoGrupi, minPoGrupi); // ukupno: racunica za minimum = broj grupa * minimum po grupi, dok je maximum = broj grupa * kapacitet grupe
    }

    //get (citaj) http metoda za sve grupe turnira
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}")]
    public async Task<IActionResult> GetByTurnir(int turnirId)
    {
        // vraca grupe sa ekipama (id i naziv), po oznakama a,b,c,d,itd.
        var grupe = await _context.Grupe
            .Where(g => g.TurnirId == turnirId)
            .OrderBy(g => g.Oznaka)
            .Select(g => new
            {
                g.GrupaId,
                g.Oznaka,
                Ekipe = g.GrupaEkipe.Select(ge => new { ge.EkipaId, ge.Ekipa.Naziv })
            })
            .ToListAsync();
        return Ok(grupe);
    }

    //post (salji) metoda za nasumicno rasporedjivanje
    [HttpPost("generisi/{turnirId}")]
    public async Task<IActionResult> Generisi(int turnirId)
    {
        //ucitava turnir uz prijavljene ekipe
        var turnir = await _context.Turniri
            .Include(t => t.TurnirEkipe)
            .FirstOrDefaultAsync(t => t.TurnirId == turnirId);

        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        //grupa se moze samo jednom napraviti.
        if (await _context.Grupe.AnyAsync(g => g.TurnirId == turnirId))
            return Conflict(new { message = "Grupe su već generisane." });

        var ekipe = turnir.TurnirEkipe.Select(te => te.EkipaId).ToList();
        var (minEkipa, maxEkipa, minPoGrupi) = Granice(turnir);

        // proverava ako prijavljene ekipe mogu stati u grupe
        if (ekipe.Count < minEkipa || ekipe.Count > maxEkipa)
            return BadRequest(new { message = $"Broj prijavljenih ekipa ({ekipe.Count}) mora biti između {minEkipa} i {maxEkipa}. Svaka od {turnir.BrojGrupa} grupa mora imati bar {minPoGrupi} ekipe, a najviše {turnir.BrojEkipaPoGrupi}." });

        //nasumicno rasporedjivanje. ako je N nedeljiv sa G, prve grupe imaju ekipu vise
        var rng = new Random();
        ekipe = ekipe.OrderBy(x => rng.Next()).ToList(); //mesaju se ekipe

        for (int i = 0; i < turnir.BrojGrupa; i++)
        {
            var grupa = new Grupa { TurnirId = turnirId, Oznaka = Oznake[i].ToString() };
            _context.Grupe.Add(grupa);

            // grupa i uzima svaku ekipu od BrojGrupa krenuci od i. ako turnir ima 4 grupe, grupa A naprimer uzima nultu, cetvrtu, osmu ekipu itd.
            for (int j = i; j < ekipe.Count; j += turnir.BrojGrupa)
                _context.GrupaEkipe.Add(new GrupaEkipa { Grupa = grupa, EkipaId = ekipe[j] });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Grupe uspešno generisane." });
    }

    // post http metoda u kome admin/organizator pravi grupe
    [HttpPost("generisi-rucno/{turnirId}")]
    public async Task<IActionResult> GenerisiRucno(int turnirId, [FromBody] RasporedDto dto)
    {
        var turnir = await _context.Turniri
            .Include(t => t.TurnirEkipe)
            .FirstOrDefaultAsync(t => t.TurnirId == turnirId);

        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        if (await _context.Grupe.AnyAsync(g => g.TurnirId == turnirId))
            return Conflict(new { message = "Grupe su već generisane." });

        var ekipe = turnir.TurnirEkipe.Select(te => te.EkipaId).ToList();
        var (minEkipa, maxEkipa, minPoGrupi) = Granice(turnir);

        if (ekipe.Count < minEkipa || ekipe.Count > maxEkipa)
            return BadRequest(new { message = $"Broj prijavljenih ekipa ({ekipe.Count}) mora biti između {minEkipa} i {maxEkipa}. Svaka od {turnir.BrojGrupa} grupa mora imati bar {minPoGrupi} ekipe, a najviše {turnir.BrojEkipaPoGrupi}." });

        var stavke = dto.Raspored ?? new List<RasporedStavka>();

        //prva provera za raspored: mora sadrzati svaku prijavljenu ekipu samo jednom
        if (stavke.Count != ekipe.Count
            || stavke.Select(s => s.EkipaId).Distinct().Count() != stavke.Count
            || stavke.Any(s => !ekipe.Contains(s.EkipaId)))
            return BadRequest(new { message = "Raspored mora sadržati svaku prijavljenu ekipu tačno jednom." });

        //dozvoljen broj oznaka na osnovu broj grupa
        var dozvoljene = Oznake.Take(turnir.BrojGrupa).Select(c => c.ToString()).ToList();

        //druga provera da svaka ekipa mora biti u nekoj grupi
        if (stavke.Any(s => !dozvoljene.Contains(s.Grupa ?? "")))
            return BadRequest(new { message = $"Svaka ekipa mora biti u nekoj od grupa: {string.Join(", ", dozvoljene)}." });

        //treca provera da grupe ostanu u granici minimalno i maximalne velicine
        foreach (var oznaka in dozvoljene)
        {
            int broj = stavke.Count(s => s.Grupa == oznaka);
            if (broj < minPoGrupi || broj > turnir.BrojEkipaPoGrupi)
                return BadRequest(new { message = $"Grupa {oznaka} ima {broj} ekipa, a mora imati između {minPoGrupi} i {turnir.BrojEkipaPoGrupi}." });
        }

        //kad sve provere prodju, pravi se grupa i upisuje ekipe po rasporedu
        foreach (var oznaka in dozvoljene)
        {
            var grupa = new Grupa { TurnirId = turnirId, Oznaka = oznaka };
            _context.Grupe.Add(grupa);

            foreach (var s in stavke.Where(s => s.Grupa == oznaka))
                _context.GrupaEkipe.Add(new GrupaEkipa { Grupa = grupa, EkipaId = s.EkipaId });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Grupe uspešno napravljene po tvom rasporedu." });
    }

    //put http metoda za editovanje sadrzaja. zamenjuju se dve ekipe iz razlicitih grupa, ali samo dok mecevi nisu generisani
    [HttpPut("zameni/{turnirId}")]
    public async Task<IActionResult> Zameni(int turnirId, [FromBody] ZamenaDto dto)
    {
        //kad se mecevi generisu, sastav grupa se zakljucava
        if (await _context.Mecevi.AnyAsync(m => m.TurnirId == turnirId))
            return BadRequest(new { message = "Mečevi su već generisani, sastav grupa se više ne može menjati." });

        if (dto.Ekipa1Id == dto.Ekipa2Id)
            return BadRequest(new { message = "Izaberi dve različite ekipe." });

        //trazi veze izmedju ekipa i grupe za obe ekipe u turniru
        var clanovi = await _context.GrupaEkipe
            .Include(ge => ge.Grupa)
            .Where(ge => ge.Grupa.TurnirId == turnirId
                && (ge.EkipaId == dto.Ekipa1Id || ge.EkipaId == dto.Ekipa2Id))
            .ToListAsync();

        if (clanovi.Count != 2)
            return BadRequest(new { message = "Obe ekipe moraju biti u grupama ovog turnira." });

        var a = clanovi[0];
        var b = clanovi[1];

        if (a.GrupaId == b.GrupaId)
            return BadRequest(new { message = "Obe ekipe su u istoj grupi, zamena nema efekta." });

        // ekipa a prelazi u grupu ekipe b i obrnuto
        (a.GrupaId, b.GrupaId) = (b.GrupaId, a.GrupaId);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Ekipe su zamenile grupe." });
    }

    // post (salji) metoda za zakljucavanje grupne faze i potvrda plasmana
    [HttpPost("zakljuci/{turnirId}")]
    public async Task<IActionResult> Zakljuci(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        if (turnir.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Grupna faza je već zaključana." });

        //samo mecevi grupne faze
        var mecevi = await _context.Mecevi
            .Where(m => m.TurnirId == turnirId && m.Faza == MecFaza.Grupna)
            .ToListAsync();

        if (mecevi.Count == 0)
            return BadRequest(new { message = "Mečevi grupne faze još nisu generisani." });

        //moze da se zakljuca grupna faza kad se svi mecevi zavrse
        if (mecevi.Any(m => m.Status != MecStatus.Zavrsena))
            return BadRequest(new { message = "Svi mečevi grupne faze moraju biti završeni (odloženi se računaju kao neodigrani)." });

        //ako izjednacenje predje granicu prolaza, admin mora da odredi ko ide dalje
        var grupe = await _context.Grupe
            .Where(g => g.TurnirId == turnirId)
            .Include(g => g.GrupaEkipe).ThenInclude(ge => ge.Ekipa)
            .Include(g => g.Mecevi)
            .OrderBy(g => g.Oznaka)
            .ToListAsync();

        foreach (var grupa in grupe)
        {
            var tabela = TabelaRacun.Izracunaj(grupa, turnir);
            int n = turnir.BrojKvalifikovanih; //koliko ekipa iz grupe prolazi dalje
            if (n >= tabela.Count) continue; //prolaze sve ekipe bez granice

            //zadnja ekipa koja prolazi i prva ekipa koja ispada
            var poslednjiProlazi = tabela[n - 1];
            var prviIspada = tabela[n];
            if (poslednjiProlazi.Izjednacenje != null
                && poslednjiProlazi.Izjednacenje == prviIspada.Izjednacenje
                && !poslednjiProlazi.RucnoOdredjen)
                return BadRequest(new { message = $"U grupi {grupa.Oznaka} je izjednačenje preko granice prolaza. Odredi redosled izjednačenih ekipa pa zaključaj grupnu fazu." }); //ako su ekipa koja prolazi i ispada u izjednacenju, a admin ne odredi ko ide dalje, daje gresku
        }

        turnir.GrupnaFazaZakljucana = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Grupna faza je zaključana. Plasman je konačan." });
    }

    // post (salji) metoda otkljucavanja dok eliminacija nije generisana
    [HttpPost("otkljucaj/{turnirId}")]
    public async Task<IActionResult> Otkljucaj(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        if (!turnir.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Grupna faza nije zaključana." });

        //ne moze da se otkljuca jer eliminacioni kostur je vec generisan
        if (await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == turnirId))
            return BadRequest(new { message = "Eliminaciona faza je već generisana (ili odbaci predlog kostura pa otključaj)." });

        turnir.GrupnaFazaZakljucana = false;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Grupna faza je otključana." });
    }
}
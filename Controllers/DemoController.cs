using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;
using TurnirSystem.Services;

//kontroler sa alatima za demo. moze praviti test ekipe i nasumicne rezultate u mecevima radi lakseg testiranja
[Authorize] //po defaultu je samo prijavljen admin
[ApiController]
[Route("api/[controller]")] //ime klase bez Controller = /api/Demo
public class DemoController : ControllerBase
{
    private const string Prefiks = "Test Ekipa "; //odabrano ime test ekipa radi lakseg prepoznavanja
    private readonly ApplicationDbContext _context; //pristup bazi

    public DemoController(ApplicationDbContext context)
    {
        _context = context;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class BrojDto
    {
        public int Broj { get; set; }
    }

    //post (salji) metoda. pravimo onoliko test ekipa koliko izaberemo
    [HttpPost("ekipe")]
    public async Task<IActionResult> DodajEkipe([FromBody] BrojDto dto)
    {
        if (dto.Broj < 1 || dto.Broj > 64) //max 64, a minimum 1
            return BadRequest(new { message = "Broj test ekipa mora biti između 1 i 64." });

        //proverava koje test ekipe vec postoje da se ne bi unele iste ekipe
        var postojeci = await _context.Ekipe
            .Where(e => e.Naziv.StartsWith(Prefiks))
            .Select(e => e.Naziv)
            .ToListAsync();

        //trazi se najveci broj test ekipe, pa ide dalje
        int max = 0;
        foreach (var naziv in postojeci)
        {
            if (int.TryParse(naziv.Substring(Prefiks.Length), out var broj) && broj > max)
                max = broj;
        }

        //petlja koja pravi ekipe. njihovo poreklo je Demo
        for (int i = 1; i <= dto.Broj; i++)
        {
            _context.Ekipe.Add(new Ekipa
            {
                Naziv = $"{Prefiks}{max + i}",
                SkraceniNaziv = $"T{max + i}",
                Mesto = "Demo"
            });
        }

        await _context.SaveChangesAsync(); //upisuje se u bazu
        return Ok(new { message = $"Dodato {dto.Broj} test ekipa (od {max + 1} do {max + dto.Broj})." }); //poruka koliko ekipa je dodato u listi
    }

    //post (salji) metoda. prijava test ekipa u turnir
    [HttpPost("prijavi-sve/{turnirId}")]
    public async Task<IActionResult> PrijaviSve(int turnirId, [FromQuery] int? broj)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako turnir ne postoji

        //ako su grupe generisane, ekipe se ne mozu vise prijaviti
        if (await _context.Grupe.AnyAsync(g => g.TurnirId == turnirId))
            return BadRequest(new { message = "Grupe su već generisane, ekipe se više ne mogu prijavljivati." });

        var prijavljeni = await _context.TurnirEkipe
            .Where(te => te.TurnirId == turnirId)
            .Select(te => te.EkipaId)
            .ToListAsync();

        //koliko ima mesta u turniru. ako je 0 onda je puno, i daje gresku
        int slobodno = turnir.BrojGrupa * turnir.BrojEkipaPoGrupi - prijavljeni.Count;
        if (slobodno <= 0)
            return BadRequest(new { message = "Turnir je već popunjen." });

        //prijavljuju se onoliko test ekipa koliko ima slobodnih mesta u turniru
        var kandidati = await _context.Ekipe
            .Where(e => e.Naziv.StartsWith(Prefiks) && !prijavljeni.Contains(e.EkipaId))
            .OrderBy(e => e.EkipaId)
            .Take(broj.HasValue && broj.Value > 0 ? Math.Min(broj.Value, slobodno) : slobodno)
            .ToListAsync();

        if (kandidati.Count == 0)
            return BadRequest(new { message = "Nema slobodnih test ekipa. Prvo ih dodaj na stranici Ekipe." }); //greska ako ne postoje test ekipe

        //prijavlju se redom od 1 do #
        foreach (var e in kandidati)
            _context.TurnirEkipe.Add(new TurnirEkipa { TurnirId = turnirId, EkipaId = e.EkipaId });

        await _context.SaveChangesAsync();

        string poruka = $"Prijavljeno {kandidati.Count} test ekipa.";
        if (!broj.HasValue && kandidati.Count < slobodno)
            poruka += $" Do popune turnira fali još {slobodno - kandidati.Count}, dodaj još test ekipa."; //moze da se dodaju jos onoliko test ekipa

        return Ok(new { message = poruka });
    }

    //post (salji) metoda. dugme da se postave nasumicni rezultati na meceve grupne faze
    [HttpPost("rezultati-grupe/{turnirId}")]
    public async Task<IActionResult> RezultatiGrupe(int turnirId)
    {
        var turnirGrupe = await _context.Turniri.FindAsync(turnirId);
        if (turnirGrupe == null)
            return NotFound(new { message = "Turnir nije pronađen." }); //greska ako turnir ne postoji

        //ako je grupna faza vec zakljucana, ne moze da se postave rezultati
        if (turnirGrupe.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Grupna faza je zaključana." });

        //ako je elim. faza vec generisana, to znaci da je grupna faza vec zakljucana i ne moze da se postave rezultati
        if (await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == turnirId))
            return BadRequest(new { message = "Eliminaciona faza je već generisana, rezultati grupne faze su zaključani." });

        //dugme moze samo da se izvrsi na neodigrane meceve
        var mecevi = await _context.Mecevi
            .Where(m => m.TurnirId == turnirId && m.Faza == MecFaza.Grupna && m.Status != MecStatus.Zavrsena)
            .ToListAsync();

        if (mecevi.Count == 0)
            return BadRequest(new { message = "Nema neodigranih mečeva grupne faze." }); //ne radi dugme jer nema vise neodigranih meceva

        //nasumicni rezultati se drze pravilnika. pobednik mora imati 3 poena, dok gubitnik moze samo do 2
        foreach (var m in mecevi)
        {
            var (a, b) = PravilaRezultata.Simuliraj(Random.Shared);
            m.RezultatEkipa1 = a;
            m.RezultatEkipa2 = b;
            m.Status = MecStatus.Zavrsena;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Uneti nasumični rezultati za {mecevi.Count} mečeva." }); //uspesna poruka za unete rezultate
    }

    //post (salji) metoda. dugme da se postave nasumicni rezultati na meceve eliminacione faze
    [HttpPost("rezultati-eliminacije/{turnirId}")]
    public async Task<IActionResult> RezultatiEliminacije(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako turnir ne postoji

        var kola = await _context.EliminacionaKola //ucitavanje meceva eliminacije ovog turnira iz baze
            .Where(k => k.TurnirId == turnirId)
            .ToListAsync();

        if (kola.Count == 0)
            return BadRequest(new { message = "Eliminaciona faza još nije generisana." }); //greska ako nema jos elim. faze

        if (!turnir.KosturPotvrdjen)
            return BadRequest(new { message = "Kostur još nije potvrđen." }); //ne mogu da se postave rezultate ako nema potvrdjenog kostura

        //kolo 0 je mec za trece mesto
        var treceMesto = kola.FirstOrDefault(k => k.BrojKola == 0);

        //svi mecevi koji jos nisu dobili rezultat. finale dobija svoj nasumicni rezultat tek na kraju
        var zaIgru = kola
            .Where(k => !k.JeBye && k.Ekipa1Id != null && k.Ekipa2Id != null && k.PobednikId == null
                && !(k.BrojKola == 1 && treceMesto != null && treceMesto.PobednikId == null))
            .OrderByDescending(k => k.BrojKola) //od prvog kola prema finalu
            .ThenBy(k => k.Pozicija)
            .ToList();

        if (zaIgru.Count == 0)
            return BadRequest(new { message = "Nema mečeva koji čekaju rezultat." }); //kad je gotovo finale, nema vise meceva

        //svaki mec dobija nasumican rezultat
        foreach (var k in zaIgru)
        {
            var (a, b) = PravilaRezultata.Simuliraj(Random.Shared);
            k.RezultatEkipa1 = a;
            k.RezultatEkipa2 = b;

            //pobednik nastavlja, gubitnik je potreban za trece mesto
            int pobednikId = a > b ? k.Ekipa1Id!.Value : k.Ekipa2Id!.Value;
            int gubitnikId = a > b ? k.Ekipa2Id!.Value : k.Ekipa1Id!.Value;
            k.PobednikId = pobednikId;

            //pobednik ide u sledece kolo (BrojKola - 1) na poziciju (pozicija + 1) / 2. neparna pozicija ide na Ekipa1, parna na Ekipa2
            if (k.BrojKola > 1)
            {
                var sledece = kola.First(x => x.BrojKola == k.BrojKola - 1 && x.Pozicija == (k.Pozicija + 1) / 2);
                if (k.Pozicija % 2 == 1) sledece.Ekipa1Id = pobednikId;
                else sledece.Ekipa2Id = pobednikId;
            }

            //gubitnik polufinala ide u mec za trece mesto
            if (k.BrojKola == 2 && treceMesto != null)
            {
                if (k.Pozicija == 1) treceMesto.Ekipa1Id = gubitnikId;
                else treceMesto.Ekipa2Id = gubitnikId;
            }
        }

        //turnir je gotov kad se odigra finale i trece mesto
        var finale = kola.First(k => k.BrojKola == 1);
        if (finale.PobednikId != null && (treceMesto == null || treceMesto.PobednikId != null))
        {
            turnir.Status = TurnirStatus.Zavrsen;
            if (turnir.DatumZavrsetka == null)
                turnir.DatumZavrsetka = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Uneti nasumični rezultati za {zaIgru.Count} mečeva." });
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;
using TurnirSystem.Services;

//kontroler za meceve. koristi se za generisanje (round robin), unos rezultata, termine ili odlaganje
[Authorize] // po defaultu je samo prijavljen admin, a javna metoda ima [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] // ime klase bez "Controller" = /api/Mecevi
public class MeceviController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public MeceviController(ApplicationDbContext context)
    {
        _context = context;
    }

    // DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class RezultatDto //rezultat koji klijent(admin) salje
    {
        public int RezultatEkipa1 { get; set; }
        public int RezultatEkipa2 { get; set; }
    }

    public class TerminDto //admin odredjuje termin. ista funkcija kao klasa terminkoladto, ali se menja ime zbog konflict u backend
    {
        public DateTime? DatumVreme { get; set; } //ako je null, nema termina
        public bool Odlozen { get; set; } //true se daje ako je mec odlozen
    
        public bool Potvrdjeno { get; set; } //true ako admin potvrdi da svejedno cuva termin nakon upozorenja
    }

    // get citaj metoda za prikaz turnira
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}")]
    public async Task<IActionResult> GetByTurnir(int turnirId) //daje listu meceva sa imena ekipa i oznake grupe (ne ceo model, samo sta je potrebno za prikaz meceva turnira). where je filter meceva turnira, include ucitava potrebne ekipe i grupe, dok select oblikuje odgovor za potrebna polja
    {
        var mecevi = await _context.Mecevi
            .Where(m => m.TurnirId == turnirId)
            .Include(m => m.Ekipa1)
            .Include(m => m.Ekipa2)
            .Include(m => m.Grupa)
            .Select(m => new
            {
                m.MecId,
                m.Faza,
                m.Status,
                m.DatumVreme,
                m.RezultatEkipa1,
                m.RezultatEkipa2,
                Ekipa1 = m.Ekipa1.Naziv,
                Ekipa2 = m.Ekipa2.Naziv,
                Grupa = m.Grupa != null ? m.Grupa.Oznaka : null //null je ako eliminacione meceve nema grupe
            })
            .ToListAsync();
        return Ok(mecevi);
    }

    //post (salji) metoda za generisanje grupne faze
    [HttpPost("generisi/{turnirId}")]
    public async Task<IActionResult> Generisi(int turnirId)
    {
        var grupe = await _context.Grupe
            .Where(g => g.TurnirId == turnirId)
            .Include(g => g.GrupaEkipe)
            .ToListAsync();

        //mora prvo grupe da se naprave da bi se pravili mecevi
        if (!grupe.Any())
            return BadRequest(new { message = "Prvo generiši grupe." });

        //mecevi mogu samo jednom da se generisu
        var postojeMecevi = await _context.Mecevi.AnyAsync(m => m.TurnirId == turnirId && m.Faza == MecFaza.Grupna);
        if (postojeMecevi)
            return Conflict(new { message = "Mečevi su već generisani." });

        //round robin metoda generisanja. svako sa svakim u grupi
        foreach (var grupa in grupe)
        {
            var ekipe = grupa.GrupaEkipe.Select(ge => ge.EkipaId).ToList();
            for (int i = 0; i < ekipe.Count; i++)
            {
                for (int j = i + 1; j < ekipe.Count; j++) //pravi parove grupa. vodi obzir da se svaki par samo jednom, i da ekipa ne igraju sa samom sobom
                {
                    _context.Mecevi.Add(new Mec
                    {
                        TurnirId = turnirId,
                        GrupaId = grupa.GrupaId,
                        Ekipa1Id = ekipe[i],
                        Ekipa2Id = ekipe[j],
                        Faza = MecFaza.Grupna,
                        Status = MecStatus.Zakazana //nov mec je zakazan, rezultat treba biti unesen
                    });
                }
            }
        }

        //status turnira se menja u 'U Toku' na Turniri karticu
        var turnirZaUpdate = await _context.Turniri.FindAsync(turnirId);
        if (turnirZaUpdate != null)
            turnirZaUpdate.Status = TurnirStatus.UToku;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Mečevi uspešno generisani." }); //poruka da su mecevi generisani ako nema greske
    }

    //put http metoda za editovanje sadrzaja. unos ili izmena rezultata grupnog meca
    [HttpPut("{id}/rezultat")]
    public async Task<IActionResult> UnosRezultata(int id, [FromBody] RezultatDto dto)
    {
        var mec = await _context.Mecevi.FindAsync(id);
        if (mec == null) return NotFound(new { message = "Meč nije pronađen." });

        //pravilnik za svaki turnir da pobednik mora da ima 3 poena, a drugi tim od 0-2
        var greska = PravilaRezultata.Validiraj(dto.RezultatEkipa1, dto.RezultatEkipa2);
        if (greska != null)
            return BadRequest(new { message = greska });

        //kada se zakljuca grupna faza, rezultati vise ne mogu se menjati
        var turnirMeca = await _context.Turniri.FindAsync(mec.TurnirId);
        if (turnirMeca != null && turnirMeca.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Grupna faza je zaključana, rezultati se više ne mogu menjati. Otključaj grupnu fazu ako treba da ispraviš rezultat." });

        //kada se eliminacioni kostur generise, dok postoji eliminacija, rezultati grupne faze se ne mogu menjati
        var postojiEliminacija = await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == mec.TurnirId);
        if (postojiEliminacija)
            return BadRequest(new { message = "Eliminaciona faza je već generisana, rezultati grupne faze se više ne mogu menjati." });

        mec.RezultatEkipa1 = dto.RezultatEkipa1;
        mec.RezultatEkipa2 = dto.RezultatEkipa2;
        mec.Status = MecStatus.Zavrsena;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Rezultat unet." }); //obicna poreka za uspesan unos
    }

    //put http metoda za editovanje sadrzaja. postavljanje termina i odlaganje
    [HttpPut("{id}/termin")]
    public async Task<IActionResult> PostaviTermin(int id, [FromBody] TerminDto dto)
    {
        var mec = await _context.Mecevi.FindAsync(id);
        if (mec == null) return NotFound(new { message = "Meč nije pronađen." }); //greska ako ne moze naci mec

        if (mec.Status == MecStatus.Zavrsena)
            return BadRequest(new { message = "Meč je već odigran, termin se više ne može menjati." }); //logicno nakon odigranog meca, termin ne moze da se menja

        //naredba koja daje upozorenje o preklapanju, tj ekipa vec igra mec blizu tog termin. nece proveriti ako je mec u kojem vec igra odlozen, a admin moze da sacuva bezobzira na upozorenje
        if (dto.DatumVreme.HasValue && !dto.Odlozen && !dto.Potvrdjeno)
        {
            var preklapanja = await ProveraTermina.Preklapanja(
                _context, dto.DatumVreme.Value, new[] { mec.Ekipa1Id, mec.Ekipa2Id }, "grupa", mec.MecId);

            //naredba za upozorenje, pita da li da se sacuva
            if (preklapanja.Count > 0)
                return Conflict(new
                {
                    message = "Termin se preklapa sa drugim mečem.",
                    potrebnaPotvrda = true,
                    trajanjeMinuta = ProveraTermina.TrajanjeMinuta,
                    preklapanja
                });
        }

        //cuvamo vreme uneseno. ne pretvara se u utc.
        mec.DatumVreme = dto.DatumVreme.HasValue
            ? DateTime.SpecifyKind(dto.DatumVreme.Value, DateTimeKind.Unspecified)
            : null;
        mec.Status = dto.Odlozen ? MecStatus.Odlozena : MecStatus.Zakazana;

        await _context.SaveChangesAsync();
        return Ok(new { message = dto.Odlozen ? "Meč je označen kao odložen." : "Termin je sačuvan." }); //ako je mec oznacen kao odlozen, termin ce se sacuvati
    }
}
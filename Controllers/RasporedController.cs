using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za raspored. spaja sve razlicite vrste meceva u jednu listu po terminu
[ApiController] //nema [Authorize], raspored je javan
[Route("api/[controller]")] //ime klase bez Controller = /api/Raspored
public class RasporedController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public RasporedController(ApplicationDbContext context)
    {
        _context = context;
    }

    //pretvara broj kola u naziv. 1 je finale, 2 polufinale, a 0 je mec za trece mesto
    private static string NazivKola(int brojKola) => brojKola switch
    {
        0 => "Meč za treće mesto",
        1 => "Finale",
        2 => "Polufinale",
        3 => "Četvrtfinale",
        4 => "Osmina finala",
        5 => "Šesnaestina finala",
        6 => "Tridesetdrugo finale",
        _ => $"Kolo {brojKola}"
    };

    //redovi u rasporedu. isti izgled za grupne i eliminacione meceve da se ne razlikuju
    private class RasporedRed
    {
        public int TurnirId { get; set; } //id turnira
        public string Turnir { get; set; } = ""; //ime turnira
        public string Faza { get; set; } = ""; //koja je faza u pitanju
        public string Ekipa1 { get; set; } = ""; //prva ekipa u mecu
        public string Ekipa2 { get; set; } = ""; //druga ekipa u mecu
        public int? RezultatEkipa1 { get; set; } //rezultat ekipe1. moze biti prazno ako mec jos nije odigran
        public int? RezultatEkipa2 { get; set; } //rezultat ekipe2. moze biti prazno ako mec jos nije odigran
        public DateTime? DatumVreme { get; set; } //termin meca (ako postoji)
        public int Status { get; set; } //status meca. moze biti zakazan, zavrsen ili odlozen (0,1,2)
    }

    //get citaj metoda. spisak rasporeda po id, ali ako nije unesen id salje sve meceve
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? turnirId)
    {
        var grupniUpit = _context.Mecevi.AsQueryable(); //AsQueryable znaci da upit jos nije izvrsen, pa se mogu dodati filteri
        if (turnirId.HasValue)
            grupniUpit = grupniUpit.Where(m => m.TurnirId == turnirId.Value);

        //ucitavaju se grupni mecevi
        var grupni = await grupniUpit
            .Select(m => new
            {
                m.TurnirId,
                Turnir = m.Turnir.Naziv,
                Grupa = m.Grupa != null ? m.Grupa.Oznaka : null,
                Ekipa1 = m.Ekipa1.Naziv,
                Ekipa2 = m.Ekipa2.Naziv,
                m.RezultatEkipa1,
                m.RezultatEkipa2,
                m.DatumVreme,
                m.Status
            })
            .ToListAsync();

        //ucitavaju se eliminacioni mecevi. ne prikazuju se bye mecevi posto ti mecevi ne postoje
        var elimUpit = _context.EliminacionaKola.Where(k => !k.JeBye);
        if (turnirId.HasValue)
            elimUpit = elimUpit.Where(k => k.TurnirId == turnirId.Value);

        var elim = await elimUpit
            .Where(k => (k.Ekipa1Id != null && k.Ekipa2Id != null) || k.DatumVreme != null) //prikazuje se samo ako su poznate obe ekipe ili je zakazan termin
            .Select(k => new
            {
                k.TurnirId,
                Turnir = k.Turnir.Naziv,
                k.BrojKola,
                Ekipa1 = k.Ekipa1 != null ? k.Ekipa1.Naziv : null,
                Ekipa2 = k.Ekipa2 != null ? k.Ekipa2.Naziv : null,
                k.RezultatEkipa1,
                k.RezultatEkipa2,
                k.DatumVreme,
                k.PobednikId,
                k.Odlozen
            })
            .ToListAsync();

        var redovi = new List<RasporedRed>();  //spajaju se oba spiska u jedan

        //grupni mecevi idu u listu
        foreach (var m in grupni)
        {
            redovi.Add(new RasporedRed
            {
                TurnirId = m.TurnirId,
                Turnir = m.Turnir,
                Faza = m.Grupa != null ? $"Grupna faza, grupa {m.Grupa}" : "Grupna faza",
                Ekipa1 = m.Ekipa1,
                Ekipa2 = m.Ekipa2,
                RezultatEkipa1 = m.RezultatEkipa1,
                RezultatEkipa2 = m.RezultatEkipa2,
                DatumVreme = m.DatumVreme,
                Status = (int)m.Status
            });
        }

        //eliminacioni mecevi idu u listu
        foreach (var k in elim)
        {
            bool odigran = k.PobednikId != null; //eliminacioni mec je odigran samo kad ima pobednika
            redovi.Add(new RasporedRed
            {
                TurnirId = k.TurnirId,
                Turnir = k.Turnir,
                Faza = NazivKola(k.BrojKola),
                Ekipa1 = k.Ekipa1 ?? "Još nije poznato", //daje ovaj tekst ako ekipa jos nije poznata
                Ekipa2 = k.Ekipa2 ?? "Još nije poznato",
                RezultatEkipa1 = odigran ? k.RezultatEkipa1 : null, //prikazuje se rezultat samo kad je mec odigran
                RezultatEkipa2 = odigran ? k.RezultatEkipa2 : null,
                DatumVreme = k.DatumVreme,
                Status = odigran ? (int)MecStatus.Zavrsena : (k.Odlozen ? (int)MecStatus.Odlozena : (int)MecStatus.Zakazana)
            });
        }

        //redosled rasporeda: prvo idu mecevi sa unesenim terminom od najranijeg do najkasnijeg, a na kraju svi koji nemaju termin
        var rezultat = redovi
            .OrderBy(r => r.DatumVreme == null)
            .ThenBy(r => r.DatumVreme)
            .ThenByDescending(r => r.TurnirId)
            .ToList();

        return Ok(rezultat); //vraca gotov raspored klijentu
    }
}

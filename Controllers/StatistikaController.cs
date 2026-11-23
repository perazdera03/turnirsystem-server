using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za statistiku. ne cuva podatke, samo racuna ono sto vec postoji. ima samo 3 get metode i to je za kontrolnu tablu, jedan turnir i jednu ekipu
[ApiController] //nema [Authorize], statistika je javna
[Route("api/[controller]")] //ime klase bez Controller = /api/Statistika
public class StatistikaController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi

    public StatistikaController(ApplicationDbContext context)
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

    //prikaz jednog reda istorije jedne ekipe. sve meceve koje je ekipa igrala
    private class IstorijaRed
    {
        public int TurnirId { get; set; } // id turnira
        public string Turnir { get; set; } = ""; //ime turnira
        public string Faza { get; set; } = ""; //da li je u grupnoj fazi ili eliminacioni
        public string Rival { get; set; } = ""; //protivnik
        public int? Moji { get; set; } //poeni od strane ekipe koju gledamo statistiku
        public int? Protivnik { get; set; } //poeni od protivnika s kim je ekipa igrala
        public string Ishod { get; set; } = ""; //da li je mec ekipe pobeda,poraz,odlozen ili zakazan
        public DateTime? DatumVreme { get; set; } //ako je odredjen termin tog meca
        public int Redosled { get; set; } //sluzi za redosled. prvo se gleda grupna faza pa eliminaciona faza
    }

    //get citaj metoda. prikaz broja na pocetnoj stranici kontrolne table
    [HttpGet("kontrolna-tabla")]
    public async Task<IActionResult> KontrolnaTabla()
    {
        var turniri = await _context.Turniri.OrderByDescending(t => t.TurnirId).ToListAsync(); //prikazuje prvo od najnovijeg turnira

        //grupise koliko je ekipa prijavljeno na svaki turnir
        var ekipePoTurniru = await _context.TurnirEkipe
            .GroupBy(te => te.TurnirId)
            .Select(g => new { TurnirId = g.Key, Broj = g.Count() })
            .ToDictionaryAsync(x => x.TurnirId, x => x.Broj);

        //za svaki turnir grupise koliko ima grupnih meceva i koliko je odigrano
        var grupniPoTurniru = await _context.Mecevi
            .GroupBy(m => m.TurnirId)
            .Select(g => new
            {
                TurnirId = g.Key,
                Ukupno = g.Count(),
                Odigrano = g.Count(m => m.Status == MecStatus.Zavrsena)
            })
            .ToDictionaryAsync(x => x.TurnirId);

        //isto za eliminacione mecove, ali ne racuna BYE jer ti mecevi ne postoje
        var elimPoTurniru = await _context.EliminacionaKola
            .Where(k => !k.JeBye)
            .GroupBy(k => k.TurnirId)
            .Select(g => new
            {
                TurnirId = g.Key,
                Ukupno = g.Count(),
                Odigrano = g.Count(k => k.PobednikId != null)
            })
            .ToDictionaryAsync(x => x.TurnirId);

        //racuna koliko ima ukupno meceva u turniru. racuna i eliminacionu i grupnu fazu
        int Ukupno(int id) =>
            (grupniPoTurniru.TryGetValue(id, out var g) ? g.Ukupno : 0) +
            (elimPoTurniru.TryGetValue(id, out var e) ? e.Ukupno : 0);

        //racuna koliko je ukupno meceva odigrano
        int Odigrano(int id) =>
            (grupniPoTurniru.TryGetValue(id, out var g) ? g.Odigrano : 0) +
            (elimPoTurniru.TryGetValue(id, out var e) ? e.Odigrano : 0);

        //svi turniri koji nisu jos zavrseni. listaju se prvo oni koji su jos u toku, onda oni koji su tek u pripremi (jos nema grupne faze)
        var aktivni = turniri
            .Where(t => t.Status != TurnirStatus.Zavrsen)
            .OrderByDescending(t => t.Status)
            .Select(t => new
            {
                t.TurnirId,
                t.Naziv,
                Status = (int)t.Status,
                t.DatumPocetka,
                BrojEkipa = ekipePoTurniru.TryGetValue(t.TurnirId, out var br) ? br : 0,
                MaxEkipa = t.BrojGrupa * t.BrojEkipaPoGrupi,
                UkupnoMeceva = Ukupno(t.TurnirId),
                Odigrano = Odigrano(t.TurnirId)
            })
            .ToList();

        //daje listu 10 najnovijih odigranih meceva u grupnoj fazi. lista se po id jer se ne cuva vreme unos rezultata
        var grupniRez = await _context.Mecevi
            .Where(m => m.Status == MecStatus.Zavrsena)
            .OrderByDescending(m => m.MecId)
            .Take(10)
            .Select(m => new
            {
                m.TurnirId,
                Turnir = m.Turnir.Naziv,
                Ekipa1 = m.Ekipa1.Naziv,
                Ekipa2 = m.Ekipa2.Naziv,
                m.RezultatEkipa1,
                m.RezultatEkipa2
            })
            .ToListAsync();

        //isto se daje lista 10 najnovijih odigranih meceva u elim. fazi. lista se po id jer se ne cuva vreme unos rezultata
        var elimRez = await _context.EliminacionaKola
            .Where(k => !k.JeBye && k.PobednikId != null && k.Ekipa1 != null && k.Ekipa2 != null)
            .OrderByDescending(k => k.EliminacionoKoloId)
            .Take(10)
            .Select(k => new
            {
                k.TurnirId,
                Turnir = k.Turnir.Naziv,
                k.BrojKola,
                Ekipa1 = k.Ekipa1!.Naziv,
                Ekipa2 = k.Ekipa2!.Naziv,
                k.RezultatEkipa1,
                k.RezultatEkipa2
            })
            .ToListAsync();

        //spajaju se obe liste za grupnu i eliminacionu fazu u jednu listu. od toga uzima 10 najnovijih meceva u celoj toj listi
        var poslednji = elimRez
            .Select(r => new
            {
                r.TurnirId,
                r.Turnir,
                Faza = NazivKola(r.BrojKola),
                r.Ekipa1,
                r.Ekipa2,
                r.RezultatEkipa1,
                r.RezultatEkipa2
            })
            .Concat(grupniRez.Select(r => new
            {
                r.TurnirId,
                r.Turnir,
                Faza = "Grupna faza",
                r.Ekipa1,
                r.Ekipa2,
                r.RezultatEkipa1,
                r.RezultatEkipa2
            }))
            .Take(10)
            .ToList();

        //odgovor klijentu
        return Ok(new
        {
            ukupnoTurnira = turniri.Count,
            uPripremi = turniri.Count(t => t.Status == TurnirStatus.Priprema),
            uToku = turniri.Count(t => t.Status == TurnirStatus.UToku),
            zavrseno = turniri.Count(t => t.Status == TurnirStatus.Zavrsen),
            ukupnoEkipa = await _context.Ekipe.CountAsync(),
            ukupnoMeceva = turniri.Sum(t => Ukupno(t.TurnirId)),
            odigranoMeceva = turniri.Sum(t => Odigrano(t.TurnirId)),
            aktivniTurniri = aktivni,
            poslednjiRezultati = poslednji
        });
    }

    //get citaj metoda. statistika jednog turnira
    [HttpGet("turnir/{turnirId}")]
    public async Task<IActionResult> PoTurniru(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako turnir ne postoji

        //sve ekipe koje su prijavljene na taj turnir
        var prijavljene = await _context.TurnirEkipe
            .Where(te => te.TurnirId == turnirId)
            .Select(te => new { te.EkipaId, te.Ekipa.Naziv })
            .ToListAsync();

        //ucitava sve meceve u tom turniru iz obe faze.
        var mecevi = await _context.Mecevi.Where(m => m.TurnirId == turnirId).ToListAsync();
        var kola = await _context.EliminacionaKola.Where(k => k.TurnirId == turnirId).ToListAsync();

        //svi odigrani mecevi. ne racuna se bye jer ti mecevi ne postoje
        var grupniOdigrani = mecevi.Where(m => m.Status == MecStatus.Zavrsena).ToList();
        var elimMecevi = kola.Where(k => !k.JeBye).ToList();
        var elimOdigrani = elimMecevi.Where(k => k.PobednikId != null).ToList();

        //zbir poena obe ekipe u jednom mecu
        int Zbir(int? a, int? b) => (a ?? 0) + (b ?? 0);

        int poeniGrupni = grupniOdigrani.Sum(m => Zbir(m.RezultatEkipa1, m.RezultatEkipa2));
        int poeniElim = elimOdigrani.Sum(k => Zbir(k.RezultatEkipa1, k.RezultatEkipa2));

        //brojevi za gornji deo stranice kontrolne table
        int ukupnoMeceva = mecevi.Count + elimMecevi.Count;
        int odigrano = grupniOdigrani.Count + elimOdigrani.Count;
        int odlozeno = mecevi.Count(m => m.Status == MecStatus.Odlozena)
            + elimMecevi.Count(k => k.Odlozen && k.PobednikId == null);

        //mecevi po fazama. racuna se svaka grupa u grupnoj fazi i svako kolo eliminacione faze
        var poFazama = new List<object>();
        if (mecevi.Count > 0)
        {
            poFazama.Add(new
            {
                faza = "Grupna faza",
                ukupno = mecevi.Count,
                odigrano = grupniOdigrani.Count,
                odlozeno = mecevi.Count(m => m.Status == MecStatus.Odlozena),
                poeni = poeniGrupni
            });
        }
        foreach (var g in elimMecevi.GroupBy(k => k.BrojKola).OrderByDescending(g => g.Key))
        {
            poFazama.Add(new
            {
                faza = NazivKola(g.Key), //kolo za eliminacionu fazu
                ukupno = g.Count(),
                odigrano = g.Count(k => k.PobednikId != null),
                odlozeno = g.Count(k => k.Odlozen && k.PobednikId == null),
                poeni = g.Where(k => k.PobednikId != null).Sum(k => Zbir(k.RezultatEkipa1, k.RezultatEkipa2))
            });
        }

        //za svaki mec koji je odigran se upisuju poeni obe ekipe radi lakseg racunanja poena, pobede i poraza. racuna se obe faze
        var ucinci = new List<(int EkipaId, int Moji, int Protivnik)>();
        foreach (var m in grupniOdigrani)
        {
            ucinci.Add((m.Ekipa1Id, m.RezultatEkipa1 ?? 0, m.RezultatEkipa2 ?? 0));
            ucinci.Add((m.Ekipa2Id, m.RezultatEkipa2 ?? 0, m.RezultatEkipa1 ?? 0));
        }
        foreach (var k in elimOdigrani)
        {
            if (k.Ekipa1Id.HasValue) ucinci.Add((k.Ekipa1Id.Value, k.RezultatEkipa1 ?? 0, k.RezultatEkipa2 ?? 0));
            if (k.Ekipa2Id.HasValue) ucinci.Add((k.Ekipa2Id.Value, k.RezultatEkipa2 ?? 0, k.RezultatEkipa1 ?? 0));
        }

        //ko je izgubio u kom kolu. koristi se za plasman, na primer gubitnik finala je drugi
        var izgubio = new Dictionary<int, int>();
        foreach (var k in elimOdigrani.Where(k => k.BrojKola != 0))
        {
            int? gubitnik = k.PobednikId == k.Ekipa1Id ? k.Ekipa2Id : k.Ekipa1Id;
            if (gubitnik.HasValue) izgubio[gubitnik.Value] = k.BrojKola;
        }

        //cetvrto mesto je gubitnik meca za trece mesto
        var treceMecOdigran = elimOdigrani.FirstOrDefault(k => k.BrojKola == 0);
        int? treceId = treceMecOdigran?.PobednikId;
        int? cetvrtoId = treceMecOdigran == null
            ? null
            : (treceMecOdigran.PobednikId == treceMecOdigran.Ekipa1Id ? treceMecOdigran.Ekipa2Id : treceMecOdigran.Ekipa1Id);
        bool treceMecPostoji = kola.Any(k => k.BrojKola == 0);

        //ekipe koje su u eliminacionom kosturu
        var uKosturu = new HashSet<int>();
        foreach (var k in kola)
        {
            if (k.Ekipa1Id.HasValue) uKosturu.Add(k.Ekipa1Id.Value);
            if (k.Ekipa2Id.HasValue) uKosturu.Add(k.Ekipa2Id.Value);
        }

        //pobednik turnira je pobednik finala (prvo mesto)
        var finale = kola.FirstOrDefault(k => k.BrojKola == 1);
        int? prvak = finale?.PobednikId;
        bool kosturGenerisan = kola.Count > 0;

        //za svaku ekipu se odredjuje status. rang je broj koji se sortira, a sto manji broj je bolji u plasmanu
        var plasman = prijavljene.Select(e =>
        {
            var moje = ucinci.Where(u => u.EkipaId == e.EkipaId).ToList();
            int pobede = moje.Count(u => u.Moji > u.Protivnik);
            int porazi = moje.Count(u => u.Moji < u.Protivnik);

            string status;
            double rang;
            if (prvak == e.EkipaId)
            {
                status = "Pobednik turnira";
                rang = -1;
            }
            else if (treceId == e.EkipaId)
            {
                status = "Treće mesto";
                rang = 1.5;
            }
            else if (cetvrtoId == e.EkipaId)
            {
                status = "Četvrto mesto";
                rang = 1.6;
            }
            else if (izgubio.TryGetValue(e.EkipaId, out var kolo))
            {
                if (kolo == 1)
                    status = "Finalista (drugo mesto)";
                else if (kolo == 2 && treceMecPostoji)
                    status = "Igra za treće mesto";
                else if (kolo == 2 && !treceMecPostoji && prvak != null)
                    status = "Treće mesto";
                else
                    status = $"Ispao u: {NazivKola(kolo)}";
                rang = kolo == 1 ? 1 : (kolo == 2 ? 2 : kolo);
            }
            else if (uKosturu.Contains(e.EkipaId))
            {
                status = "U igri";
                rang = 0;
            }
            else if (kosturGenerisan)
            {
                status = "Ispao u grupnoj fazi";
                rang = 100;
            }
            else if (mecevi.Count > 0) //ako nema kostura, ekipa je jos u grupnoj fazi
            {
                status = "Grupna faza";
                rang = 100;
            }
            else //ako nema kostura, ekipa je samo prijavljena
            {
                status = "Prijavljena"; 
                rang = 100;
            }
            return new //lista statistike ekipe
            {
                ekipaId = e.EkipaId,
                naziv = e.Naziv,
                status,
                rang,
                odigrano = moje.Count,
                pobede,
                porazi,
                poeni = moje.Sum(u => u.Moji),
                primljeni = moje.Sum(u => u.Protivnik)
            };
        })
        //sortiranje liste po rangu, onda pobede, poena dobijenih - primljenih pa onda po nazivu.
        .OrderBy(x => x.rang)
        .ThenByDescending(x => x.pobede)
        .ThenByDescending(x => x.poeni - x.primljeni)
        .ThenBy(x => x.naziv)
        .ToList();

        return Ok(new //lista statistike turnira
        {
            turnir = new { turnir.TurnirId, turnir.Naziv, Status = (int)turnir.Status },
            brojEkipa = prijavljene.Count,
            maxEkipa = turnir.BrojGrupa * turnir.BrojEkipaPoGrupi,
            ukupnoMeceva,
            odigrano,
            preostalo = ukupnoMeceva - odigrano,
            odlozeno,
            ukupnoPoena = poeniGrupni + poeniElim,
            poFazama,
            plasman
        });
    }

    //get citaj metoda. istorija meceva jedne ekipe
    [HttpGet("ekipa/{ekipaId}")]
    public async Task<IActionResult> PoEkipi(int ekipaId)
    {
        var ekipa = await _context.Ekipe.FindAsync(ekipaId);
        if (ekipa == null) return NotFound(new { message = "Ekipa nije pronađena." }); //greska ako ekipa ne postoji

        //grupni mecevi ekipa. nije bitno da li je ekipa prva ili druga u mecu
        var grupni = await _context.Mecevi
            .Where(m => m.Ekipa1Id == ekipaId || m.Ekipa2Id == ekipaId)
            .Select(m => new
            {
                m.TurnirId,
                Turnir = m.Turnir.Naziv,
                m.Ekipa1Id,
                Ekipa1 = m.Ekipa1.Naziv,
                Ekipa2 = m.Ekipa2.Naziv,
                m.RezultatEkipa1,
                m.RezultatEkipa2,
                m.Status,
                m.DatumVreme
            })
            .ToListAsync();

        //eliminacioni mecevi ekipe. nije bitno da li je ekipa prva ili druga u mecu. bye mecevi se ne racunaju
        var elim = await _context.EliminacionaKola
            .Where(k => !k.JeBye && (k.Ekipa1Id == ekipaId || k.Ekipa2Id == ekipaId))
            .Select(k => new
            {
                k.TurnirId,
                Turnir = k.Turnir.Naziv,
                k.BrojKola,
                k.Ekipa1Id,
                Ekipa1 = k.Ekipa1 != null ? k.Ekipa1.Naziv : null,
                Ekipa2 = k.Ekipa2 != null ? k.Ekipa2.Naziv : null,
                k.RezultatEkipa1,
                k.RezultatEkipa2,
                k.PobednikId,
                k.Odlozen,
                k.DatumVreme
            })
            .ToListAsync();

        //spajaju se obe faze u jednu listu
        var redovi = new List<IstorijaRed>();

        foreach (var m in grupni)
        {
            //ako je prva true to znaci da je ekipa bila ekipa1 u mecu i zna se koji rezultat je njen u grupnoj fazi
            bool prva = m.Ekipa1Id == ekipaId;
            bool odigran = m.Status == MecStatus.Zavrsena;
            int? moji = odigran ? (prva ? m.RezultatEkipa1 : m.RezultatEkipa2) : null;
            int? protivnik = odigran ? (prva ? m.RezultatEkipa2 : m.RezultatEkipa1) : null;

            string ishod;
            if (odigran)
                ishod = moji > protivnik ? "Pobeda" : "Poraz";
            else
                ishod = m.Status == MecStatus.Odlozena ? "Odloženo" : "Zakazano";

            redovi.Add(new IstorijaRed //istorija meceva
            {
                TurnirId = m.TurnirId,
                Turnir = m.Turnir,
                Faza = "Grupna faza",
                Rival = prva ? m.Ekipa2 : m.Ekipa1,
                Moji = moji,
                Protivnik = protivnik,
                Ishod = ishod,
                DatumVreme = m.DatumVreme,
                Redosled = 0
            });
        }

        foreach (var k in elim)
        {
            //ako je prva true to znaci da je ekipa bila ekipa1 u mecu i zna se koji rezultat je njen u elim. fazi
            bool prva = k.Ekipa1Id == ekipaId;
            bool odigran = k.PobednikId != null;
            int? moji = odigran ? (prva ? k.RezultatEkipa1 : k.RezultatEkipa2) : null;
            int? protivnik = odigran ? (prva ? k.RezultatEkipa2 : k.RezultatEkipa1) : null;

            string ishod;
            if (odigran)
                ishod = k.PobednikId == ekipaId ? "Pobeda" : "Poraz";
            else
                ishod = k.Odlozen ? "Odloženo" : "Zakazano";

            redovi.Add(new IstorijaRed //istorija meceva
            {
                TurnirId = k.TurnirId,
                Turnir = k.Turnir,
                Faza = NazivKola(k.BrojKola),
                Rival = (prva ? k.Ekipa2 : k.Ekipa1) ?? "Još nije poznat",
                Moji = moji,
                Protivnik = protivnik,
                Ishod = ishod,
                DatumVreme = k.DatumVreme,
                Redosled = 100 - k.BrojKola
            });
        }

        //listaju se najnoviji turniri prvo, onda po redosledu, i onda terminu
        var meceviLista = redovi
            .OrderByDescending(r => r.TurnirId)
            .ThenBy(r => r.Redosled)
            .ThenBy(r => r.DatumVreme)
            .ToList();

        //broje se samo odigrani mecevi
        var odigrani = meceviLista.Where(r => r.Moji != null).ToList();

        return Ok(new
        {
            ekipa = new { ekipa.EkipaId, ekipa.Naziv },
            odigrano = odigrani.Count,
            pobede = odigrani.Count(r => r.Ishod == "Pobeda"),
            porazi = odigrani.Count(r => r.Ishod == "Poraz"),
            poeni = odigrani.Sum(r => r.Moji ?? 0),
            primljeni = odigrani.Sum(r => r.Protivnik ?? 0),
            mecevi = meceviLista //vraca listu
        });
    }
}
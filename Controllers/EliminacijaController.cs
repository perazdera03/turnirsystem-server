using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;
using TurnirSystem.Services;

//kontroler za elim. fazu turnira. poseduje generisanje kostura (predlog), zamena parova, termine i unos rezultata
[Authorize] // po defaultu je samo prijavljen admin, a javna metoda ima [AllowAnonymous]
[ApiController]
[Route("api/[controller]")] // ime klase bez "Controller" = /api/Eliminacija
public class EliminacijaController : ControllerBase
{
    private readonly ApplicationDbContext _context; // pristup bazi

    public EliminacijaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class RezultatEliminacijeDto //salje poene prve i druge ekipe
    {
        public int RezultatEkipa1 { get; set; }
        public int RezultatEkipa2 { get; set; }
    }

    public class ZamenaKosturaDto //zamena dve ekipe u kostur
    {
        public int Ekipa1Id { get; set; }
        public int Ekipa2Id { get; set; }
    }

    public class TerminKolaDto //admin odredjuje termin. ista funkcija kao klasa termindto, ali se menja ime zbog konflict u backend
    {
        public DateTime? DatumVreme { get; set; } //ako je null, nema termina
        public bool Odlozen { get; set; } //true se daje ako je mec odlozen
        
        public bool Potvrdjeno { get; set; } //true ako admin potvrdi da svejedno cuva termin nakon upozorenja
    }

    private class Kvalifikovana  //ekipa koja prolazi grupnu fazu. vodi se obzir id, grupa i plasman u grupi (1 = prvoplasirana)
    {
        public int EkipaId { get; set; }
        public int GrupaId { get; set; }
        public int Plasman { get; set; }
        //prepisuje se ou, bodovi, razlika i poeni iz tabele grupa. sluze za rang listu, to jest ko je nosilac
        public int OU { get; set; }
        public int Bodovi { get; set; }
        public int Razlika { get; set; }
        public int Poeni { get; set; }
    }

    private static string NazivKolaGenitiv(int brojKola) => brojKola switch //naziv kola u genitiv, sluzi za oznake (primer, pobednik cetvrtfinala 1)
    {
        1 => "finala",
        2 => "polufinala",
        3 => "četvrtfinala",
        4 => "osmine finala",
        5 => "šesnaestine finala",
        6 => "tridesetdruge finala",
        _ => $"kola {brojKola}"
    };

    private static string Oznaka(EliminacionoKolo k, int slot) //oznaka za mesto u kostur koje jos nije popunjeno (slot 1-2)
    {
        if (k.BrojKola == 0) return $"Gubitnik polufinala {slot}"; //mec za trece mesto. igraju iskljucivo gubitnici polufinala

        int izvorPozicija = 2 * k.Pozicija - 2 + slot;
        return $"Pobednik {NazivKolaGenitiv(k.BrojKola + 1)} {izvorPozicija}"; //od kog polja iz proslog kola dolazi ekipa. polje P se napaja iz polja 2p-1 i 2p (obrnuto od pravila (P+1)/2)
    }

    // get citaj metoda prikaz celog kostura, od prvog kola do finala (i meca za trece mesto)
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}")]
    public async Task<IActionResult> GetByTurnir(int turnirId) //ucitava sva kola kostura, od levo na desno. prvo kolo je skroz levo (najvise meceva), a finale skroz desno (jedan mec)
    {
        var kola = await _context.EliminacionaKola
            .Where(k => k.TurnirId == turnirId)
            .Include(k => k.Ekipa1)
            .Include(k => k.Ekipa2)
            .Include(k => k.Pobednik)
            .OrderByDescending(k => k.BrojKola)
            .ThenBy(k => k.Pozicija)
            .ToListAsync();

        var rezultat = kola.Select(k => new //funkcija za prazno mesto. ekipa jos nije poznata i inicijalno pise 'pobednik # #'. ako je BYE metoda iskorisnjena, pise tekst 'BYE'
        {
            k.EliminacionoKoloId,
            k.BrojKola,
            k.Pozicija,
            k.JeBye,
            Ekipa1 = k.Ekipa1 != null ? k.Ekipa1.Naziv : Oznaka(k, 1),
            Ekipa1Id = k.Ekipa1Id,
            Ekipa2 = k.JeBye ? "BYE" : (k.Ekipa2 != null ? k.Ekipa2.Naziv : Oznaka(k, 2)),
            Ekipa2Id = k.Ekipa2Id,
            Pobednik = k.Pobednik?.Naziv,
            k.PobednikId,
            k.RezultatEkipa1,
            k.RezultatEkipa2,
            k.DatumVreme,
            Odlozen = k.Odlozen && k.PobednikId == null, //odlozeno se jedino prikazuje dok mec nije odigran
            k.MecId
        });

        return Ok(rezultat);
    }

    // get citaj metoda pitanje da li je kostur generisan i potvrdjen
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}/status")]
    public async Task<IActionResult> Status(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        var generisan = await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == turnirId);
        return Ok(new { generisan, potvrdjen = generisan && turnir.KosturPotvrdjen }); //ako je kostur generisan, onda ce postojati u bazi. potvrdjen znaci da je admin potvrdio, ali do tada je samo predlog
    }

    // get citaj metoda - prvo, drugo, trece i cetvrto mesto. u zavisnosti od kostura, moze i bez treceg/cetvrtog mesta
    [AllowAnonymous] //svi mogu videti, ali ne mogu menjati
    [HttpGet("{turnirId}/plasman")]
    public async Task<IActionResult> Plasman(int turnirId)
    {
        var kola = await _context.EliminacionaKola
            .Include(k => k.Ekipa1)
            .Include(k => k.Ekipa2)
            .Where(k => k.TurnirId == turnirId)
            .ToListAsync();

        var finale = kola.FirstOrDefault(k => k.BrojKola == 1); //plasman se zna samo kad je odigrano finale

        if (finale == null || finale.PobednikId == null)
            return Ok(new { zavrsen = false });

        //prvo mesto je pobednik finala, drugo je gubitnik finala
        var prvi = finale.PobednikId == finale.Ekipa1Id ? finale.Ekipa1 : finale.Ekipa2;
        var drugi = finale.PobednikId == finale.Ekipa1Id ? finale.Ekipa2 : finale.Ekipa1;

        //trece i cetvrto mesto zavise od meca za trece mesto (BrojKola 0)
        object? treci = null;
        object? cetvrti = null;

        var treceMesto = kola.FirstOrDefault(k => k.BrojKola == 0);

        if (treceMesto != null)
        {
            if (treceMesto.PobednikId != null)
            {
                var t = treceMesto.PobednikId == treceMesto.Ekipa1Id ? treceMesto.Ekipa1 : treceMesto.Ekipa2;
                var c = treceMesto.PobednikId == treceMesto.Ekipa1Id ? treceMesto.Ekipa2 : treceMesto.Ekipa1;
                treci = new { t!.EkipaId, t.Naziv };
                cetvrti = new { c!.EkipaId, c.Naziv };
            }
        }
        else
        {
            //druga varijanta da ako polufinale sa bye ima samo jednog gubitnika, dobice trece mesto
            var polufinale = kola.Where(k => k.BrojKola == 2 && !k.JeBye && k.PobednikId != null).ToList();
            if (polufinale.Count == 1)
            {
                var p = polufinale[0];
                var gubitnik = p.PobednikId == p.Ekipa1Id ? p.Ekipa2 : p.Ekipa1;
                if (gubitnik != null)
                    treci = new { gubitnik.EkipaId, gubitnik.Naziv };
            }
        }

        return Ok(new
        {
            zavrsen = true,
            prvi = new { prvi!.EkipaId, prvi.Naziv },
            drugi = new { drugi!.EkipaId, drugi.Naziv },
            treci,
            cetvrti
        });
    }

    //post (salji) metoda. pravi se kostur od jednom kao predlog, ali nije potvrdjen
    [HttpPost("generisi/{turnirId}")]
    public async Task<IActionResult> Generisi(int turnirId)
    {
        var turnir = await _context.Turniri
            .Include(t => t.Grupe)
            .FirstOrDefaultAsync(t => t.TurnirId == turnirId);

        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako se trazi turnir koji ne postoji

        //kostur se generise samo jednom. ako vec postoji (predlog ili potvrdjen), ne pravi se novi
        var postojiEliminacija = await _context.EliminacionaKola.AnyAsync(k => k.TurnirId == turnirId);
        if (postojiEliminacija)
            return Conflict(new { message = "Eliminaciona faza je već generisana." });

        if (!turnir.GrupnaFazaZakljucana)
            return BadRequest(new { message = "Prvo zaključaj grupnu fazu (potvrdi konačan plasman)." }); //uslov da grupna faza mora biti zakljucana 

        var nezavrseniMecevi = await _context.Mecevi
            .AnyAsync(m => m.TurnirId == turnirId && m.Faza == MecFaza.Grupna && m.Status != MecStatus.Zavrsena);
        if (nezavrseniMecevi)
            return BadRequest(new { message = "Svi mečevi grupne faze moraju biti završeni pre generisanja eliminacije." }); //jos jedan uslov. ako nisu zavrseni svi mecevi grupne faze, ne moze da se generise eliminacioni kostur

        //ekipe svake grupe koje su kvalifikovane. plasman je isto dodat
        var kvalifikovane = new List<Kvalifikovana>();
        foreach (var grupa in turnir.Grupe)
        {
            var tabela = await UcitajTabelu(grupa.GrupaId, turnir);
            int plasman = 1;
            foreach (var red in tabela.Take(turnir.BrojKvalifikovanih))
            {
                kvalifikovane.Add(new Kvalifikovana
                {
                    EkipaId = red.EkipaId,
                    GrupaId = grupa.GrupaId,
                    Plasman = plasman++,
                    OU = red.OU,
                    Bodovi = red.Bod,
                    Razlika = red.Razlika,
                    Poeni = red.Poeni
                });
            }
        }

        if (kvalifikovane.Count < 2)
            return BadRequest(new { message = "Za eliminacionu fazu su potrebne bar 2 kvalifikovane ekipe." }); //ako je mali broj ekipa u turnir, mora imati bar 2 kvalifikovana u jednoj grupi ili jedan kvalifikovan u dve grupe

        //rang lista. prikazuje plasman ekipa, pa onda prosek po odigranim mecevima (bod,razlika,poeni). prosek je bitan jer grupe se mogu razlikovati po velicine, pa onda tako i broj meceva
        var rang = kvalifikovane
            .OrderBy(k => k.Plasman)
            .ThenByDescending(k => k.OU > 0 ? (double)k.Bodovi / k.OU : 0.0)
            .ThenByDescending(k => k.OU > 0 ? (double)k.Razlika / k.OU : 0.0)
            .ThenByDescending(k => k.OU > 0 ? (double)k.Poeni / k.OU : 0.0)
            .ThenBy(k => k.GrupaId)
            .ThenBy(k => k.EkipaId)
            .ToList();

        //n je broj kvalifikovanih. kostur se pravi po velicini prvi stepen dvojke koji je veci ili jednak n. ako se pravi kostur od 8 a ima 6 ekipa, dve ce biti BYE
        int n = rang.Count;
        int velicina = 1;                       //velicina kostura je sledeci stepen broja 2
        while (velicina < n) velicina *= 2;
        int brojKola = (int)Math.Log2(velicina); //broj kola prvog kola. finale je broj 1 i krece unazad

        //raspored nosioca. najbolja ekipa igrace protiv najslabije. prva i druga ekipa su uvek u suprotne polovine kostura, a redosled za 8 ekipa bi bilo 1-8,4-5,2-7,3-6
        var redosled = new List<int> { 1 };
       
        while (redosled.Count < velicina) //naredba koja duplira raspored kako bi na primer 1 i 2 bili u suprotne polovine
        {
            int nova = redosled.Count * 2;
            var sledeci = new List<int>();
            foreach (var s in redosled)
            {
                sledeci.Add(s);
                sledeci.Add(nova + 1 - s);
            }
            redosled = sledeci;
        }

        //nosilac (broj) -> ekipa. nosioci veci od broja ekipa su prazna mesta, a njihov protivnik dobija BYE
        var naSedistu = new Kvalifikovana?[velicina + 1];
        for (int i = 0; i < n; i++) naSedistu[i + 1] = rang[i];

        //koliko mesta ima u kosturu. mesta u 2p i 2p+1 igraju u prvom kolu. ako je prazno mesto, onda je bye za susedno mesto
        var mesta = new Kvalifikovana?[velicina];
        for (int i = 0; i < velicina; i++) mesta[i] = naSedistu[redosled[i]];

       //koliko ce se ekipa iz iste grupe sresti u svakom kolu, samo se finale ne racuna. mesto i i j se srecu u kolu koja je duzina bita (i xor j): 1=prvo kolo, 2=drugo, itd
        int[] Trosak()
        {
            var brojac = new int[brojKola + 1];
            for (int i = 0; i < velicina; i++)
            {
                if (mesta[i] == null) continue;
                for (int j = i + 1; j < velicina; j++)
                {
                    if (mesta[j] == null || mesta[i]!.GrupaId != mesta[j]!.GrupaId) continue;
                    //u koje kolo se mesta i i j srecu (XOR mesta: manji broj bitova = ranije kolo)
                    int nivo = System.Numerics.BitOperations.Log2((uint)(i ^ j)) + 1;
                    if (nivo < brojKola) brojac[nivo]++;
                }
            }
            return brojac;
        }

        //bolje je kada ima manje susreta u prvim kolima. ide od pocetnog kola do finala
        bool Bolji(int[] a, int[] b)
        {
            for (int k = 1; k < a.Length; k++)
            {
                if (a[k] != b[k]) return a[k] < b[k];
            }
            return false;
        }

        // ako je susedno mesto prazno, mesto ce biti BYE
        bool ImaBye(int mesto) => mesta[mesto ^ 1] == null;

        //izbegavanje ekipa iz iste grupe u kasnijim kolima. menjaju se samo ekipa iz istih plasmana. bye ekipe se ne diraju
        var trenutni = Trosak();
       
        for (int prolaz = 0; prolaz < 200; prolaz++) //poboljsavanje da u svakom prolazu se uzima najbolja zamena. moze najvise 200 prolaza
        {
            int najboljiI = -1, najboljiJ = -1;
            var najbolji = trenutni;

            for (int i = 0; i < velicina; i++)
            {
                if (mesta[i] == null) continue;
                for (int j = i + 1; j < velicina; j++)
                {
                    if (mesta[j] == null) continue;
                    if (mesta[i]!.Plasman != mesta[j]!.Plasman) continue; //menjaju se samo ekipe istog plasman. prvoplasirani ostaju prvoplasirani
                    if (mesta[i]!.GrupaId == mesta[j]!.GrupaId) continue;
                    if (ImaBye(i) != ImaBye(j)) continue;
                    if (i / 2 == j / 2) continue; //isti mec, zamena nista ne menja

                    //probna zamena. izracuna se trosak, pa se zamena vrati nazad. Prava zamena se radi tek posle petlje
                    (mesta[i], mesta[j]) = (mesta[j], mesta[i]);
                    var t = Trosak();
                    (mesta[i], mesta[j]) = (mesta[j], mesta[i]);

                    if (Bolji(t, najbolji))
                    {
                        najbolji = t;
                        najboljiI = i;
                        najboljiJ = j;
                    }
                }
            }

            if (najboljiI == -1) break; //nema zamene vise koje pomazu

            (mesta[najboljiI], mesta[najboljiJ]) = (mesta[najboljiJ], mesta[najboljiI]);
            trenutni = najbolji;
        }

        //kola od pocetka do finala se prave od jednom
        var kolaMapa = new Dictionary<(int Kolo, int Pozicija), EliminacionoKolo>();
        for (int kolo = brojKola; kolo >= 1; kolo--)
        {
            //kolo ima 2^(k-1) meceva
            int brojMeceva = 1 << (kolo - 1);
            for (int poz = 1; poz <= brojMeceva; poz++)
            {
                var k = new EliminacionoKolo { TurnirId = turnirId, BrojKola = kolo, Pozicija = poz };
                kolaMapa[(kolo, poz)] = k;
                _context.EliminacionaKola.Add(k);
            }
        }

        //mec za trece mesto. uslov je da oba polufinala imaju pravi mec. ako ima jedan bye, mec za trece mesto ne postoji, a gubitnik drugog meca dobija trece mesto
        bool poluFinaleImaBye = brojKola == 2 && n < velicina;
        if (brojKola >= 2 && !poluFinaleImaBye)
        {
            _context.EliminacionaKola.Add(new EliminacionoKolo { TurnirId = turnirId, BrojKola = 0, Pozicija = 1 });
        }

        //upisuje ekipe u njeno mesto u sledecem kolu. ako je neparna pozicija = ekipa1, a parna = ekipa 2
        void PostaviUSledece(EliminacionoKolo k, int ekipaId)
        {
            if (k.BrojKola <= 1) return;
            var sledece = kolaMapa[(k.BrojKola - 1, (k.Pozicija + 1) / 2)];
            if (k.Pozicija % 2 == 1) sledece.Ekipa1Id = ekipaId;
            else sledece.Ekipa2Id = ekipaId;
        }

        // za prvo kolo. parovi po mestima u kosturu, a bye prosledjuje ekipu odmah
        for (int p = 1; p <= velicina / 2; p++)
        {
            var a = mesta[2 * (p - 1)];
            var b = mesta[2 * (p - 1) + 1];
            var k = kolaMapa[(brojKola, p)];

            //prva ekipa postoji uvek. Ako druga ne postoji, prva ima BYE i ide dalje
            k.Ekipa1Id = a!.EkipaId;
            if (b == null)
            {
                k.JeBye = true;
                k.PobednikId = a.EkipaId;
                PostaviUSledece(k, a.EkipaId);
            }
            else
            {
                k.Ekipa2Id = b.EkipaId;
            }
        }

        //kostur ostaje predlog dok admin ne potvrdi
        turnir.KosturPotvrdjen = false;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Kostur je generisan kao predlog. Pregledaj ga pa ga potvrdi." });
    }

    //put http metoda za editovanje sadrzaja. mogucnost zamene ekipe iz prvog kola dok kostur nije potvrdjen
    [HttpPut("zameni/{turnirId}")]
    public async Task<IActionResult> Zameni(int turnirId, [FromBody] ZamenaKosturaDto dto)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        if (turnir.KosturPotvrdjen)
            return BadRequest(new { message = "Kostur je već potvrđen, parovi se više ne mogu menjati." });

        if (dto.Ekipa1Id == dto.Ekipa2Id)
            return BadRequest(new { message = "Izaberi dve različite ekipe." });

        var kola = await _context.EliminacionaKola.Where(k => k.TurnirId == turnirId).ToListAsync();
        if (kola.Count == 0)
            return BadRequest(new { message = "Eliminaciona faza još nije generisana." });

        //moze da se uradi zamena samo u prvom kolu i samo izmedju pravih meceva bez bye
        int prvoKolo = kola.Max(k => k.BrojKola);
        var pravi = kola.Where(k => k.BrojKola == prvoKolo && !k.JeBye).ToList();

        var a = pravi.FirstOrDefault(k => k.Ekipa1Id == dto.Ekipa1Id || k.Ekipa2Id == dto.Ekipa1Id);
        var b = pravi.FirstOrDefault(k => k.Ekipa1Id == dto.Ekipa2Id || k.Ekipa2Id == dto.Ekipa2Id);

        if (a == null || b == null)
            return BadRequest(new { message = "Obe ekipe moraju igrati pravi meč u prvom kolu (ekipe sa BYE se ne mogu zamenjivati)." });

        if (a.EliminacionoKoloId == b.EliminacionoKoloId)
            return BadRequest(new { message = "Ekipe već igraju jedna protiv druge." });

        //pamti se gde se koja ekipa nalazi, da bi moglo mesta da se zauzmu pravilno
        bool prvaJeEkipa1UA = a.Ekipa1Id == dto.Ekipa1Id;
        bool drugaJeEkipa1UB = b.Ekipa1Id == dto.Ekipa2Id;

        if (prvaJeEkipa1UA) a.Ekipa1Id = dto.Ekipa2Id; else a.Ekipa2Id = dto.Ekipa2Id;
        if (drugaJeEkipa1UB) b.Ekipa1Id = dto.Ekipa1Id; else b.Ekipa2Id = dto.Ekipa1Id;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Ekipe su zamenile mesta u kosturu." });
    }

    //post (salji) metoda za potvrdu kostura. kad se potvrdi moze da se unose rezultati. ako mec prvog kola ima termin koji se preklapa, trazi se potvrda
    [HttpPost("potvrdi/{turnirId}")]
    public async Task<IActionResult> Potvrdi(int turnirId, [FromQuery] bool potvrdjeno = false)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        var kola = await _context.EliminacionaKola.Where(k => k.TurnirId == turnirId).ToListAsync();
        if (kola.Count == 0)
            return BadRequest(new { message = "Eliminaciona faza još nije generisana." });

        if (turnir.KosturPotvrdjen)
            return BadRequest(new { message = "Kostur je već potvrđen." });

        if (!potvrdjeno)
        {
            //zamena u predlog moze da prebaci ekipu u mec ciji se termin sudara sa drugim mecom
            int prvoKolo = kola.Max(k => k.BrojKola);
            var preklapanja = new List<string>();

            foreach (var k in kola.Where(k => k.BrojKola == prvoKolo
                && !k.JeBye && !k.Odlozen
                && k.DatumVreme != null
                && k.Ekipa1Id != null && k.Ekipa2Id != null))
            {
                var nadjena = await ProveraTermina.Preklapanja(
                    _context,
                    k.DatumVreme!.Value,
                    new[] { k.Ekipa1Id!.Value, k.Ekipa2Id!.Value },
                    "elim",
                    k.EliminacionoKoloId);

                preklapanja.AddRange(nadjena);
            }

            //ista upozorenja mogu vise puta se pojaviti
            preklapanja = preklapanja.Distinct().ToList();

            if (preklapanja.Count > 0)
                return Conflict(new
                {
                    message = "Neki termini u prvom kolu se preklapaju sa drugim mečevima.",
                    potrebnaPotvrda = true,
                    trajanjeMinuta = ProveraTermina.TrajanjeMinuta,
                    preklapanja
                });
        }

        //kad se potvrdi kostur, rezultati se mogu uneti, ali zato se ne moze vise menjati kostur
        turnir.KosturPotvrdjen = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Kostur je potvrđen. Možeš da unosiš rezultate." });
    }

    //delete metoda za brisanje. odbacuje se nepotvrdjen kostur, ali moze da se generise novi
    [HttpDelete("{turnirId}/predlog")]
    public async Task<IActionResult> OdbaciPredlog(int turnirId)
    {
        var turnir = await _context.Turniri.FindAsync(turnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." }); //greska ako se ne moze naci turnir

        if (turnir.KosturPotvrdjen)
            return BadRequest(new { message = "Kostur je već potvrđen i ne može se odbaciti." }); //ako je kostur potvrdjen, ne moze vise da se odbacuje

        var kola = await _context.EliminacionaKola.Where(k => k.TurnirId == turnirId).ToListAsync();
        if (kola.Count == 0)
            return BadRequest(new { message = "Nema predloga kostura." }); //greska jer ne postoji predlog koji bi se odbacio

        
        _context.EliminacionaKola.RemoveRange(kola); //ovim se brisu sva polja kostura, ali moze novi predlog da se pravi
        await _context.SaveChangesAsync();
        return Ok(new { message = "Predlog kostura je odbačen." });
    }

    //put http metoda za editovanje sadrzaja. funkcije za datum, vreme i oznaka "odlozen" za eliminacione meceve
    [HttpPut("{id}/termin")]
    public async Task<IActionResult> PostaviTermin(int id, [FromBody] TerminKolaDto dto)
    {
        var kolo = await _context.EliminacionaKola.FindAsync(id);
        if (kolo == null) return NotFound(new { message = "Meč nije pronađen." });

        //bye ne moze imati termin, jer mec ne postoji
        if (kolo.JeBye)
            return BadRequest(new { message = "BYE meč nema termin." });

        if (kolo.PobednikId != null)
            return BadRequest(new { message = "Meč je već odigran, termin se više ne može menjati." }); //greska ako se pokusa promeniti termin nakon zavrsenog meca

        //ne znamo koje su ekipe jos, pa ne moze da se odlozi
        if (dto.Odlozen && (kolo.Ekipa1Id == null || kolo.Ekipa2Id == null))
            return BadRequest(new { message = "Meč se ne može odložiti dok oba učesnika nisu određena." });

        //uslov za upozorenje ako se termini preklapaju. vodi se obzir samo za ekipe koje su poznate
        if (dto.DatumVreme.HasValue && !dto.Odlozen && !dto.Potvrdjeno)
        {
            var poznateEkipe = new[] { kolo.Ekipa1Id, kolo.Ekipa2Id }
                .Where(e => e != null)
                .Select(e => e!.Value)
                .ToList();

            var preklapanja = await ProveraTermina.Preklapanja(
                _context, dto.DatumVreme.Value, poznateEkipe, "elim", kolo.EliminacionoKoloId);

            if (preklapanja.Count > 0)
                return Conflict(new
                {
                    message = "Termin se preklapa sa drugim mečom.",
                    potrebnaPotvrda = true,
                    trajanjeMinuta = ProveraTermina.TrajanjeMinuta,
                    preklapanja
                });
        }

        //cuvamo uneto vreme bez pretvaranje u utc
        kolo.DatumVreme = dto.DatumVreme.HasValue
            ? DateTime.SpecifyKind(dto.DatumVreme.Value, DateTimeKind.Unspecified)
            : null;
        kolo.Odlozen = dto.Odlozen;

        await _context.SaveChangesAsync();
        return Ok(new { message = dto.Odlozen ? "Meč je označen kao odložen." : "Termin je sačuvan." });
    }

    //put http metoda za editovanje sadrzaja. unos ili ispravak rezultata
    [HttpPut("{id}/rezultat")]
    public async Task<IActionResult> UnosRezultata(int id, [FromBody] RezultatEliminacijeDto dto)
    {
        var kolo = await _context.EliminacionaKola.FindAsync(id);
        if (kolo == null) return NotFound(new { message = "Kolo nije pronađeno." });

        var turnir = await _context.Turniri.FindAsync(kolo.TurnirId);
        if (turnir == null) return NotFound(new { message = "Turnir nije pronađen." });

        //rezultat ne moze se uneti dok se ne potvrdi kostur
        if (!turnir.KosturPotvrdjen)
            return BadRequest(new { message = "Kostur još nije potvrđen." });

        if (kolo.JeBye)
            return BadRequest(new { message = "BYE meč nema rezultat, ekipa automatski prolazi." });

        if (kolo.Ekipa1Id == null || kolo.Ekipa2Id == null)
            return BadRequest(new { message = "Meč se ne može zaključati dok oba učesnika nisu određena." });

        // pravilnik glasi da pobednik mora imati 3 poena, dok gubitnik od 0-2. bilo sta drugo nije dozvoljeno
        var greska = PravilaRezultata.Validiraj(dto.RezultatEkipa1, dto.RezultatEkipa2);
        if (greska != null)
            return BadRequest(new { message = greska });

        //zastita turnira, ne sme biti nereseno
        if (dto.RezultatEkipa1 == dto.RezultatEkipa2)
            return BadRequest(new { message = "Eliminacioni meč mora imati pobednika." });

        //trazi sledeci mec u kosturu. ako je null, onda je finale i trece mesto
        EliminacionoKolo? sledece = null;
        if (kolo.BrojKola > 1)
        {
            sledece = await _context.EliminacionaKola.FirstOrDefaultAsync(k =>
                k.TurnirId == kolo.TurnirId &&
                k.BrojKola == kolo.BrojKola - 1 &&
                k.Pozicija == (kolo.Pozicija + 1) / 2);
        }

        //mec za trece mesto. ne postoji uvek
        var treceMesto = await _context.EliminacionaKola.FirstOrDefaultAsync(k =>
            k.TurnirId == kolo.TurnirId && k.BrojKola == 0);

        //mec za trece mesto. igra se pre finala
        if (kolo.BrojKola == 1 && treceMesto != null && treceMesto.PobednikId == null)
            return BadRequest(new { message = "Prvo se igra meč za treće mesto, pa tek onda finale." });

        //uslovna naredba da ako je finale vec odigrano, trece mesto ne moze vise da se menja
        if (kolo.BrojKola == 0)
        {
            var odigranoFinale = await _context.EliminacionaKola.AnyAsync(k =>
                k.TurnirId == kolo.TurnirId && k.BrojKola == 1 && k.PobednikId != null);
            if (odigranoFinale)
                return BadRequest(new { message = "Finale je već odigrano, rezultat trećeg mesta se više ne može menjati." });
        }

        //uslovna naredba da se moze menjati rezultat sve dok sledeci mec nije odigran
        if (kolo.PobednikId != null)
        {
            if (sledece != null && sledece.PobednikId != null)
                return BadRequest(new { message = "Sledeći meč je već odigran, rezultat se više ne može menjati." });

            if (kolo.BrojKola == 2 && treceMesto != null && treceMesto.PobednikId != null)
                return BadRequest(new { message = "Meč za treće mesto je već odigran, rezultat polufinala se više ne može menjati." });
        }

        //pobednik ide u sledece kolo jer ima vise poena. gubitnik pada. u slucaju polufinala gubitnik je potreban za mec za trece mesto.
        int pobednikId = dto.RezultatEkipa1 > dto.RezultatEkipa2 ? kolo.Ekipa1Id.Value : kolo.Ekipa2Id.Value;
        int gubitnikId = pobednikId == kolo.Ekipa1Id.Value ? kolo.Ekipa2Id.Value : kolo.Ekipa1Id.Value;

        kolo.RezultatEkipa1 = dto.RezultatEkipa1;
        kolo.RezultatEkipa2 = dto.RezultatEkipa2;
        kolo.PobednikId = pobednikId;
        kolo.Odlozen = false; //odigran mec vise nije odlozen

        if (sledece != null)
        {
            //pobednik zauzima svoje mesto u sledecem kolu
            if (kolo.Pozicija % 2 == 1) sledece.Ekipa1Id = pobednikId;
            else sledece.Ekipa2Id = pobednikId;
        }

        //gubitnik polufinala ide za trece mesto
        if (kolo.BrojKola == 2 && treceMesto != null)
        {
            if (kolo.Pozicija == 1) treceMesto.Ekipa1Id = gubitnikId;
            else treceMesto.Ekipa2Id = gubitnikId;
        }

        //turnir je gotov kad se odigra finale i mec za trece mesto
        if (kolo.BrojKola <= 1)
        {
            var finale = kolo.BrojKola == 1
                ? kolo
                : await _context.EliminacionaKola.FirstOrDefaultAsync(k => k.TurnirId == kolo.TurnirId && k.BrojKola == 1);

            bool finaleOdigrano = finale != null && finale.PobednikId != null;
            bool treceOdigrano = treceMesto == null || treceMesto.PobednikId != null;

            if (finaleOdigrano && treceOdigrano)
            {
                turnir.Status = TurnirStatus.Zavrsen;
                if (turnir.DatumZavrsetka == null)
                    turnir.DatumZavrsetka = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Rezultat unet, pobednik određen." });
    }

    //tabela grupe zajednicke logike (ista kao tabelacontroller)
    private async Task<List<RedTabele>> UcitajTabelu(int grupaId, Turnir turnir)
    {
        var grupa = await _context.Grupe
            .Include(g => g.GrupaEkipe).ThenInclude(ge => ge.Ekipa)
            .Include(g => g.Mecevi)
            .FirstAsync(g => g.GrupaId == grupaId);

        return TabelaRacun.Izracunaj(grupa, turnir);
    }
}
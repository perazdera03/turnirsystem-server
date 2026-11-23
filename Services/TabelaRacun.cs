using TurnirSystem.Models;

namespace TurnirSystem.Services;

public class RedTabele //pomocna klasa, nema bazu. odredjuje jednu ekipu u tabeli grupe. racuna se svaki put iz odigranih meceva
{
    public int EkipaId { get; set; } //id ekipe
    public string Naziv { get; set; } = ""; //obavezno, naziv ekipe
    public int OU { get; set; } //odigrane utakmice
    public int P { get; set; } //pobede
    public int I { get; set; } //izgubljene
    public int Poeni { get; set; } //osvojeni poeni u svim mecevima 
    public int PrimljeniPoeni { get; set; } //poeni koje su protivnici dali
    public int Razlika => Poeni - PrimljeniPoeni; //samo se racuna, ne cuva se
    public int Bod { get; set; } //bodovi za plasman (grupne faze)

    // admin moze rucno staviti redosled (ako je null, onda nije postavljen)
    public int? RucniPlasman { get; set; }

    // redni broj izjednacenja (ekipe koje se ne mogu razdvojiti imaju isti), null ako ekipa nije izjednacena
    public int? Izjednacenje { get; set; }

    // ako je true, admin je odredio redosled izjednacenja
    public bool RucnoOdredjen { get; set; }
}

public static class TabelaRacun //pomocna klasa, nema bazu. prima podatke i vraca listu podataka
{
    public static List<RedTabele> Izracunaj(Grupa grupa, Turnir turnir) //lista, racuna tabelu jedne grupe. potrebno imati ucitanu GrupaEkipe (sa Ekipa) i mecevi. redosled racunice je bodovi, medjusobni rezultat, ukupna razlika, ukupni poeni, rucni plasman, id.
    {
        var zavrseni = grupa.Mecevi.Where(m => m.Status == MecStatus.Zavrsena).ToList(); //ulaze samo zavrseni mecevi u tabelu

        bool grupaZavrsena = grupa.Mecevi.Count > 0 && grupa.Mecevi.All(m => m.Status == MecStatus.Zavrsena); //prikazuje izjednacenje kada su mecevi grupe zavrseni

        //celokupna naredba prebrojava meceva svih ekipa (OU,P,I,poeni,bodovi)
        var redovi = new List<RedTabele>();
        foreach (var ge in grupa.GrupaEkipe)
        {
            var red = new RedTabele
            {
                EkipaId = ge.EkipaId,
                Naziv = ge.Ekipa.Naziv,
                RucniPlasman = ge.RucniPlasman
            };

            foreach (var m in zavrseni.Where(m => m.Ekipa1Id == ge.EkipaId || m.Ekipa2Id == ge.EkipaId)) //svi zavrseni mecevi u kojima igra ova ekipa (prva pozicija ili druga)
            {

                //moji = poeni te ekipe, protivnik=poeni protivnika
                bool jeEkipa1 = m.Ekipa1Id == ge.EkipaId;
                int moji = jeEkipa1 ? (m.RezultatEkipa1 ?? 0) : (m.RezultatEkipa2 ?? 0);
                int protivnik = jeEkipa1 ? (m.RezultatEkipa2 ?? 0) : (m.RezultatEkipa1 ?? 0);

                red.OU++;
                red.Poeni += moji;
                red.PrimljeniPoeni += protivnik;

                if (moji > protivnik) red.P++;
                else red.I++;
            }

            red.Bod = red.P * turnir.BodPobeda + red.I * turnir.BodPoraz; //racunica bodova po pravilu turnira
            redovi.Add(red);
        }

        // poredjenje ekipa prvo sa bodovimo. ekipa sa istim bodovima imaju jos kriterijume
        var rezultat = new List<RedTabele>();
        int brojIzjednacenja = 0;

        foreach (var saIstimBodovima in redovi.GroupBy(r => r.Bod).OrderByDescending(g => g.Key)) //grupisanje ekipa od najvise do najmanje bodova
        {
            var ekipe = saIstimBodovima.ToList();

            if (ekipe.Count == 1) //ekipa je jedina sa odredjenim brojem bodova, ne treba da se dira
            {
                rezultat.Add(ekipe[0]);
                continue;
            }

            var ids = ekipe.Select(e => e.EkipaId).ToHashSet(); //odredjuje samo meceve koji su odigrani izmedju ekipa koji su bili izjednaceni
            var medjusobniMecevi = zavrseni
                .Where(m => ids.Contains(m.Ekipa1Id) && ids.Contains(m.Ekipa2Id))
                .ToList();

            var ocenjeni = ekipe  //racuna mini tabelu. bodovi i razlika se odredjuje samo u tim mecevima
                .Select(e => new { Red = e, Mini = MiniStatistika(e.EkipaId, medjusobniMecevi, turnir) })
                .ToList();

            var sortirano = ocenjeni //redosled kriterijuma: medjusobni bodovi, medj. razlika, ukupna razlika, ukupno poeni, rucni plasman i ID
                .OrderByDescending(x => x.Mini.Bod)
                .ThenByDescending(x => x.Mini.Razlika)
                .ThenByDescending(x => x.Red.Razlika)
                .ThenByDescending(x => x.Red.Poeni)
                .ThenBy(x => x.Red.RucniPlasman ?? int.MaxValue) // admin moze odluciti
                .ThenBy(x => x.Red.EkipaId)                      // stalan redosled
                .ToList();

            // kad se grupa zavrsi, trazi ekipe koje se ne mogu razdvojiti
            if (grupaZavrsena)
            {
                var klasteri = sortirano //ekipe koje su jednake u prvih 4 kriterijuma, ne moze se razdvojiti
                    .GroupBy(x => (x.Mini.Bod, x.Mini.Razlika, x.Red.Razlika, x.Red.Poeni))
                    .Where(g => g.Count() > 1);

                foreach (var klaster in klasteri)
                {
                    brojIzjednacenja++;

                    // redosled je odredjen kad admin postavlja plasman svih ekipa u izjednacenju
                    bool rucnoOdredjen = klaster.All(x => x.Red.RucniPlasman != null);

                    foreach (var x in klaster) //oznacavaju se da pripadaju istom izjednacenju
                    {
                        x.Red.Izjednacenje = brojIzjednacenja;
                        x.Red.RucnoOdredjen = rucnoOdredjen;
                    }
                }
            }

            rezultat.AddRange(sortirano.Select(x => x.Red));
        }

        return rezultat;
    }

    // bodovi i razlika ekipe samo u mecevima izmedju izjednacenih ekipa
    private static (int Bod, int Razlika) MiniStatistika(int ekipaId, List<Mec> mecevi, Turnir turnir)
    {
        int bod = 0, razlika = 0;

        foreach (var m in mecevi.Where(m => m.Ekipa1Id == ekipaId || m.Ekipa2Id == ekipaId))
        {
            bool jeEkipa1 = m.Ekipa1Id == ekipaId;
            int moji = jeEkipa1 ? (m.RezultatEkipa1 ?? 0) : (m.RezultatEkipa2 ?? 0);
            int protivnik = jeEkipa1 ? (m.RezultatEkipa2 ?? 0) : (m.RezultatEkipa1 ?? 0);

            razlika += moji - protivnik;

            if (moji > protivnik) bod += turnir.BodPobeda;
            else bod += turnir.BodPoraz;
        }

        return (bod, razlika);
    }
}
namespace TurnirSystem.Models;

//stanje turnira. priprema je dok se ne generisu mecevi, u toku je dok traje, a zavrsen kad se odigra finale
public enum TurnirStatus
{
    Priprema,//turnir je u pripremi
    UToku,//turnir je u toku
    Zavrsen//turnir je gotov
}

//model klasa turnira. klase u model se prevode u mysql bazu. jedan turnir sa svim podesavanjima
public class Turnir
{
    public int TurnirId {get; set;} //primarni kljuc tabele
    public string Naziv {get; set;} = ""; //ime turnira. obavezno polje koje se mora popuniti
    public string? Opis {get; set;} //opis turnira. moze biti prazno(null)
    public DateTime DatumPocetka {get; set;} //datum pocetka turnira. obavezno polje
    public DateTime? DatumZavrsetka {get; set;} //datum zavrsetka turnira. moze biti prazno(null)
    public int BrojGrupa {get; set;} //broj grupa (G). obavezno polje
    public int BrojEkipaPoGrupi {get; set;} //najvise ekipa u jednoj grupi (M). to je kapacitet, a ne obavezan broj ekipa. obavezno polje
    public int BrojKvalifikovanih {get; set;} //koliko ekipa iz svake grupe prolazi u eliminaciju. obavezno polje
    public int BodPobeda {get; set;} = 3; //bodovi za pobedu. podesavaju se po turniru, a 3 je pocetna vrednost
    public int BodPoraz {get; set;} = 0; //bodovi za poraz. 0 je pocetna vrednost
    public TurnirStatus Status {get; set;} = TurnirStatus.Priprema; //stanje turnira. turnir se pravi u pripremi

    public bool KosturPotvrdjen {get; set;} //false dok je eliminacioni kostur samo predlog, a true kad ga admin potvrdi

    public bool GrupnaFazaZakljucana {get; set;} //true kad admin zakljuca grupnu fazu. posle toga rezultati grupe ne mogu da se menjaju

    //navigacija za EF Core (C# u MySQL bazi)
    public ICollection<TurnirEkipa> TurnirEkipe {get; set;} = new List<TurnirEkipa>(); //lista prijavljenih ekipa ovog turnira
    public ICollection<Grupa> Grupe {get; set;} = new List<Grupa>(); //lista grupa ovog turnira
}

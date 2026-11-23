namespace TurnirSystem.Models;

//model klasa ekipe. klase u model se prevode u mysql bazu. sluzi za prikaz ekipa
public class Ekipa
{
    public int EkipaId {get; set;} //primarni kljuc tabele
    public string Naziv {get; set;} = ""; //ime ekipe. obavezno polje koje se mora popuni
    public string? SkraceniNaziv {get; set;} //skraceni naziv ekipe. moze biti prazno(null)
    public string? Mesto {get; set;} //poreklo ekipe. moze biti prazno(null)
    public string? KontaktOsoba {get; set;} //ime menadzera ekipe. moze biti prazno(null)
    public string? LogoUrl {get; set;} //logo ekipe. ako ekipa zeli sliku, mora dati url slike. moze biti prazno(null)
}


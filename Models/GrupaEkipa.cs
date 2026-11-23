namespace TurnirSystem.Models;

//model klasa grupaekipa. klase u model se prevode u mysql bazu. kaze koja ekipa je u kojoj grupi, a potrebna je jer je povezana sa grupom i ekipa tabele, i pravi se posebna tabela
public class GrupaEkipa
{
    public int GrupaEkipaId { get; set; } //primarni kljuc tabele
    public int GrupaId { get; set; } //strani kljuc, id grupe. obavezno polje
    public int EkipaId { get; set; } //strani kljuc, id ekipe koja je u grupi. obavezno polje

    public int? RucniPlasman { get; set; } //redosled koji admin rucno postavi kad su ekipe potpuno izjednacene (1 je bolja ekipa). moze biti prazno(null) ako nije postavljen

    //navigacija za EF Core (C# u MySQL bazi)
    public Grupa Grupa { get; set; } = null!; //grupa u kojoj je ekipa
    public Ekipa Ekipa { get; set; } = null!; //ekipa koja je u grupi
}

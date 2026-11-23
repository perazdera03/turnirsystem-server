namespace TurnirSystem.Models;

//model klasa turnirekipa. klase u model se prevode u mysql bazu. kaze koja je ekipa prijavljena na koji turnir, a potrebna je jer ekipa moze na vise turnira, a turnir ima vise ekipa
public class TurnirEkipa
{
    public int TurnirEkipaId { get; set;} //primarni kljuc tabele
    public int TurnirId {get; set;} //strani kljuc, id turnira na koji je ekipa prijavljena. obavezno polje
    public int EkipaId{get; set;} //strani kljuc, id prijavljene ekipe. obavezno polje

    //navigacija za EF Core (C# u MySQL bazi)
    public Turnir Turnir {get; set;} = null!; //turnir na koji je ekipa prijavljena
    public Ekipa Ekipa {get; set;} = null!; //ekipa koja je prijavljena
}

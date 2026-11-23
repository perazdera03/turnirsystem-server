namespace TurnirSystem.Models;

//model klasa grupe. klase u model se prevode u mysql bazu. jedna grupa unutar turnira (A,B,C,itd)
public class Grupa
{
    public int GrupaId {get; set;} //primarni kljuc tabele
    public int TurnirId {get; set;} //strani kljuc, id turnira kome grupa pripada
    public string Oznaka {get; set;} = ""; //slovo grupe (A,B,C,itd). server dodeljuje redom

    //navigacija za EF Core (C# u MySQL bazi)
    public Turnir Turnir {get; set;} = null!; //turnir kome grupa pripada. null! znaci da ce EF popuniti kad ucita grupu
    public ICollection<GrupaEkipa> GrupaEkipe {get; set;} = new List<GrupaEkipa>(); //lista ekipa koje su u ovoj grupi
    public ICollection<Mec> Mecevi {get; set;} = new List<Mec>(); //lista meceva koji se igraju u ovoj grupi
}

namespace TurnirSystem.Models;

//stanje meca
public enum MecStatus
{
    Zakazana, //mec jos nije odigran
    Zavrsena, //mec je odigran i upisan je rezultat
    Odlozena //mec je odlozen, racuna se kao neodigran
}
//da li je mec iz grupne ili eliminacione faze
public enum MecFaza
{
    Grupna, //mec grupne faze
    Eliminaciona //u ovoj tabeli se ne koristi, eliminacioni mecevi su u EliminacionoKolo
}
//model klasa meca. klase u model se prevode u mysql bazu. jedan mec grupne faze izmedju dve ekipe
public class Mec
{
    public int MecId {get; set;} //primarni kljuc tabele
    public int TurnirId {get; set;} //strani kljuc, id turnira kome mec pripada. obavezno polje
    public int? GrupaId {get; set;} //strani kljuc, id grupe u kojoj se mec igra. moze biti prazno(null) jer eliminacioni mec nema grupu
    public int Ekipa1Id {get; set;} //strani kljuc, id prve ekipe. Ekipa1 i Ekipa2 pokazuju na istu tabelu Ekipa. obavezno polje
    public int Ekipa2Id {get; set;} //strani kljuc, id druge ekipe. obavezno polje
    public int? RezultatEkipa1 { get; set; } //poeni prve ekipe. moze biti prazno(null) dok mec nije odigran
    public int? RezultatEkipa2 { get; set; } //poeni druge ekipe. moze biti prazno(null) dok mec nije odigran
    public MecStatus Status {get; set;} = MecStatus.Zakazana; //stanje meca. mec se pravi kao zakazan
    public MecFaza Faza {get; set;} //grupna ili eliminaciona faza. obavezno polje
    public DateTime? DatumVreme {get; set;} //termin meca. moze biti prazno(null), moze se uneti kasnije

    //navigacija za EF Core (C# u MySQL bazi)
    public Turnir Turnir {get; set;} = null!; //turnir kome mec pripada. ucitava se sa Include samo kad treba
    public Grupa? Grupa {get; set;} //grupa u kojoj se mec igra. moze biti prazno(null) za eliminacioni mec
    public Ekipa Ekipa1 {get; set;} = null!; //prva ekipa u meci
    public Ekipa Ekipa2 {get; set;} = null!; //druga ekipa u meci

}

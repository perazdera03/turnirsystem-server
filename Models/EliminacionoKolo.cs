namespace TurnirSystem.Models;

//model klasa eliminacionokolo. klase u model se prevode u mysql bazu. eliminaciono kolo (kostur) u turniru
public class EliminacionoKolo
{
    public int EliminacionoKoloId {get; set;} //primarni kljuc tabele
    public int TurnirId {get; set;} //kljuc turnira kome pripada eliminacija
    public int BrojKola {get; set;} //odredjuje kolo u eliminaciji. broji se od finala unazad, broj 1 je finale, broj 2 polufinale, itd. broj 0 je posebno kolo za trece mesto
    public int Pozicija {get; set;} //redni broj meca unutar kola (1, 2, 3, itd.)
    public bool JeBye {get; set;} //true oznacava ako ekipa u eliminaciji nema protivnika.
    public int? Ekipa1Id {get; set;} //prva ekipa u mecu. nepoznato dok se ne sazna ko igra
    public int? Ekipa2Id {get; set;} //druga ekipa u mecu. isto tako je nepoznato
    public int? PobednikId {get; set;} //pobednik, tj ko ide dalje
    public int? MecId {get; set;} //veza sa tabelom (modelom) Mec
    public int? RezultatEkipa1 {get; set;} //rezultati se prepisuju ovde
    public int? RezultatEkipa2 {get; set;} //i ovde

    public DateTime? DatumVreme {get; set;} //termin za meceve. moze biti i prazno polje(null)
    public bool Odlozen {get; set;} //oznacuje se ako se termin odlaze

    //navigacija za EF Core (C# u MySQL bazi)
    public Turnir Turnir {get; set;} = null!; //turnir u kom pripada eliminacija
    public Ekipa? Ekipa1 {get; set;} //prva ekipa u mecu. moze biti prazno(null) dok se ne sazna ko igra
    public Ekipa? Ekipa2 {get; set;} //druga ekipa u mecu. moze biti prazno(null) dok se ne sazna ko igra
    public Ekipa? Pobednik {get; set;} //ekipa koja je pobedila. moze biti prazno(null) dok se ne sazna ko igra
    public Mec? Mec {get; set;} //mec iz tabele Mec je povezan sa ovim kolom. moze biti prazno(null)
}
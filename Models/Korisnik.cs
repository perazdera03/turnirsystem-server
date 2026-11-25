namespace TurnirSystem.Models;

//uloga korisnika. trenutno postoji samo admin
public enum Rola
{
    Admin //jedini korisnik koji se prijavljuje i menja podatke
}

//model klasa korisnika. klase u model se prevode u mysql bazu. administrator aplikacije
public class Korisnik
{
    public int KorisnikId {get; set;} //primarni kljuc tabele
    public string Username {get; set;} = ""; //korisnicko ime za prijavu. mora biti jedinstveno (proverava se u AuthController). obavezno polje
    public string PasswordHash {get; set;} = ""; //hes lozinke. cuva se samo hash, nikad prava lozinka. obavezno polje
    public Rola Rola {get; set;} //uloga korisnika (trenutno samo Admin)
}

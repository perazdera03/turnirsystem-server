namespace TurnirSystem.Services;

public static class PravilaRezultata //pomocna klasa, nema bazu
{
    // mec se igra do 3 poena za jednu ekipu. koristi se i u rucnom unosu i demo simulaciji
    public const int PoenaZaPobedu = 3;

    // proverava unesen rezultat. ako daje null, rezultat je ispravan i ne salje poruku, ako nije salje poruku za gresku
    public static string? Validiraj(int rezultat1, int rezultat2)
    {
        // u turnirima bilo kakvog tipa, nema smisla negativnih poena
        if (rezultat1 < 0 || rezultat2 < 0)
            return "Rezultat ne može biti negativan."; 

        // osnovno pravilo da pobednik mora imati 3 poena, dok protivnik moze 0, 1, 2
        if (Math.Max(rezultat1, rezultat2) != PoenaZaPobedu || Math.Min(rezultat1, rezultat2) >= PoenaZaPobedu)
            return $"Meč se igra do {PoenaZaPobedu} poena: pobednik ima tačno {PoenaZaPobedu}, a drugi tim 0, 1 ili 2.";

        return null;
    }

    // simulacija za demo alat. koristi rng funkciju koji ako da 0 prva ekipa dobija, ako je 1 daje poen drugoj ekipi
    public static (int, int) Simuliraj(Random rng)
    {
        int a = 0, b = 0;
        while (a < PoenaZaPobedu && b < PoenaZaPobedu)
        {
            if (rng.Next(2) == 0) a++;
            else b++;
        }
        return (a, b);
    }
}
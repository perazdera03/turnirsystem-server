using Microsoft.EntityFrameworkCore;
using TurnirSystem.Data;
using TurnirSystem.Models;

namespace TurnirSystem.Services;

//pomocna klasa koja proverava ako se termini preklapaju. poziva se preko controller eliminacija kao funkcija
public static class ProveraTermina
{
    public const int TrajanjeMinuta = 60; //predvidjeno trajanje meca. ne cuva se vreme kad se zavrsava mec

    //daje spisak upozorenja. ako je prazno onda nema preklapanja
    public static async Task<List<string>> Preklapanja(
        ApplicationDbContext context,
        DateTime vreme,
        IEnumerable<int> ekipe,
        string izuzetiTip, //tip meca koji se preskace (grupa ili elim.). sluzi da se ne preklapa sa samim sobom
        int izuzetiId) //id tog meca
    {
        var rezultat = new List<string>();
        var ids = ekipe.Distinct().ToList(); //lista ekipa preko id, da se ne ponavljaju
        if (ids.Count == 0) return rezultat;

        //preklapanje je ako ista ekipa igra drugi mec u razmaku od 60 minuta pre ili posle ovog termina
        var od = vreme.AddMinutes(-TrajanjeMinuta);
        var doVreme = vreme.AddMinutes(TrajanjeMinuta);
        int izuzetiGrupni = izuzetiTip == "grupa" ? izuzetiId : -1;
        int izuzetiElim = izuzetiTip == "elim" ? izuzetiId : -1;

        //trazimo grupne meceve u tom razmaku u kojima igra neka od ovih ekipa
        var grupni = await context.Mecevi
            .Include(m => m.Ekipa1)
            .Include(m => m.Ekipa2)
            .Where(m => m.DatumVreme != null && m.DatumVreme > od 
                && m.DatumVreme < doVreme 
                && m.MecId != izuzetiGrupni 
                && m.Status == MecStatus.Zakazana //gleda se samo zakazani mecevi koji se tek igraju
                && (ids.Contains(m.Ekipa1Id) || ids.Contains(m.Ekipa2Id)))
            .ToListAsync();

        //ako se preklapaju termini, daje poruku upozorenja sa opisom ko se sa kim preklapa
        foreach (var m in grupni)
        {
            string termin = m.DatumVreme!.Value.ToString("yyyy-MM-dd HH:mm");
            string opis = $"{m.Ekipa1.Naziv} - {m.Ekipa2.Naziv}, grupni meč #{m.MecId}";
            if (ids.Contains(m.Ekipa1Id))
                rezultat.Add($"{m.Ekipa1.Naziv} već igra u {termin} ({opis}).");
            if (ids.Contains(m.Ekipa2Id))
                rezultat.Add($"{m.Ekipa2.Naziv} već igra u {termin} ({opis}).");
        }

        //isto za eliminacione meceve
        var elim = await context.EliminacionaKola
            .Include(k => k.Ekipa1)
            .Include(k => k.Ekipa2)
            .Where(k => k.DatumVreme != null && k.DatumVreme > od && k.DatumVreme < doVreme
                && !k.Odlozen && !k.JeBye //ne racunaju se mecevi koji su odlozeni ili bye mecevi
                && k.PobednikId == null
                && k.EliminacionoKoloId != izuzetiElim
                && ((k.Ekipa1Id != null && ids.Contains(k.Ekipa1Id.Value)) //ekipe mogu biti null dok se ne sazna ko igra
                    || (k.Ekipa2Id != null && ids.Contains(k.Ekipa2Id.Value))))
            .ToListAsync();

        //ako se preklapaju termini, daje poruku upozorenja sa opisom ko se sa kim preklapa
        foreach (var k in elim)
        {
            string termin = k.DatumVreme!.Value.ToString("yyyy-MM-dd HH:mm");
            string n1 = k.Ekipa1?.Naziv ?? "?";
            string n2 = k.Ekipa2?.Naziv ?? "?";
            string opis = $"{n1} - {n2}, eliminacioni meč #{k.EliminacionoKoloId}";
            if (k.Ekipa1Id != null && ids.Contains(k.Ekipa1Id.Value))
                rezultat.Add($"{n1} već igra u {termin} ({opis}).");
            if (k.Ekipa2Id != null && ids.Contains(k.Ekipa2Id.Value))
                rezultat.Add($"{n2} već igra u {termin} ({opis}).");
        }

        return rezultat.Distinct().ToList(); //brisu se duple poruke
    }
}
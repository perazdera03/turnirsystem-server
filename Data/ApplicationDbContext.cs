using Microsoft.EntityFrameworkCore;
using TurnirSystem.Models;

namespace TurnirSystem.Data;

//veza izmedju programa i baze. svaki DbSet je jedna tabela, a preko njih se citaju i pisu podaci
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { } //konstruktor prima podesavanja baze (adresa, lozinka) iz Program.cs

    public DbSet<Korisnik> Korisnici {get; set;} //tabela korisnika (admin)
    public DbSet<Ekipa> Ekipe {get; set;} //tabela ekipe
    public DbSet<Turnir> Turniri {get; set;} //tabela turniri
    public DbSet<TurnirEkipa> TurnirEkipe {get; set;} //tabela za ekipe u odredjenom turniru
    public DbSet<Grupa> Grupe {get; set;} //tabela grupe u turniru (a,b,c,d, itd)
    public DbSet<GrupaEkipa> GrupaEkipe {get; set;} //tabela ekipe u grupama
    public DbSet<Mec> Mecevi {get; set;} //tabela meceva
    public DbSet<EliminacionoKolo> EliminacionaKola {get; set;} //tabela meceva eliminacione faze (ceo kostur)

    //podesavanja veze i pravila koja ef ne moze sam prepoznati iz klasa modela
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //mec ima dve veze ka tabelama ekipa 1 i 2, pa se moraju podesiti rucno
        modelBuilder.Entity<Mec>()
            .HasOne(m => m.Ekipa1)
            .WithMany()
            .HasForeignKey(m => m.Ekipa1Id)
            .OnDelete(DeleteBehavior.Restrict); //restrict znaci da baza ne dozvoljava brisanje ekipe koja ima mec

        //mec ima dve veze ka tabelama ekipa 1 i 2, pa se moraju podesiti rucno
        modelBuilder.Entity<Mec>()
            .HasOne(m => m.Ekipa2)
            .WithMany()
            .HasForeignKey(m => m.Ekipa2Id)
            .OnDelete(DeleteBehavior.Restrict); //restrict znaci da baza ne dozvoljava brisanje ekipe koja ima mec

        //ista funkcija za eliminaciono kolo, samo sa tri veze ekipa1 ekipa2 i pobednik
        modelBuilder.Entity<EliminacionoKolo>()
            .HasOne(e => e.Ekipa1)
            .WithMany()
            .HasForeignKey(e => e.Ekipa1Id)
            .OnDelete(DeleteBehavior.Restrict);

        //ista funkcija za eliminaciono kolo, samo sa tri veze ekipa1 ekipa2 i pobednik
        modelBuilder.Entity<EliminacionoKolo>()
            .HasOne(e => e.Ekipa2)
            .WithMany()
            .HasForeignKey(e => e.Ekipa2Id)
            .OnDelete(DeleteBehavior.Restrict);
        
        //ista funkcija za eliminaciono kolo, samo sa tri veze ekipa1 ekipa2 i pobednik
        modelBuilder.Entity<EliminacionoKolo>()
            .HasOne(e => e.Pobednik)
            .WithMany()
            .HasForeignKey(e => e.PobednikId)
            .OnDelete(DeleteBehavior.Restrict);

        //jedinstveni indeks, sto znaci da ista ekipa ne moze biti prijavljena na isti turnir dva puta cak iako kod ima gresku
        modelBuilder.Entity<TurnirEkipa>()
            .HasIndex(te => new { te.TurnirId, te.EkipaId })
            .IsUnique();

        //isto tako ekipa ne moze biti u istu grupu dva puta
        modelBuilder.Entity<GrupaEkipa>()
            .HasIndex(ge => new { ge.GrupaId, ge.EkipaId })
            .IsUnique();
    }
}
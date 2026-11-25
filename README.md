# TurnirSystem (server)

Server aplikacije za organizaciju turnira: ekipe, grupna faza, tabele, eliminacioni kostur, raspored i statistika. Napisan u ASP.NET Core (C#), sa MySQL bazom i prijavom preko JWT tokena. Klijent (Vue 3) je u posebnom repozitorijumu `turnirsystem-klijent`.

## Šta je potrebno

- .NET 10 SDK
- MySQL server (npr. 8.x)
- alat za migracije: `dotnet tool install --global dotnet-ef`

## Pokretanje

1. Kopiraj `appsettings.example.json` u `appsettings.json` i upiši svoje podatke:
   - u `DefaultConnection` zameni `UPISI_LOZINKU_BAZE` lozinkom svog MySQL korisnika,
   - u `Jwt:Key` upiši tajni ključ od najmanje 32 karaktera.
2. Napravi bazu iz migracije:
   ```
   dotnet ef database update
   ```
3. Pokreni server:
   ```
   dotnet run
   ```
   Server radi na `http://localhost:5095`, a Swagger (testiranje API-ja) je na `http://localhost:5095/swagger`.

## Prvi admin nalog

U bazi na početku nema korisnika. Prvi nalog može da se napravi bez prijave, preko Swagger-a: `POST /api/Auth/register` sa telom
```json
{ "username": "admin", "password": "tvojalozinka" }
```
(ime 3 do 50 karaktera, lozinka najmanje 6). Posle toga se prijavljuješ u klijentu.

## Pravila turnira

- Meč se igra do 3 poena: pobednik ima tačno 3, drugi tim 0, 1 ili 2. Nerešenog rezultata nema.
- Bodovi za pobedu i poraz se podešavaju po turniru (početno 3 i 0).
- Tabela grupe se računa iz odigranih mečeva (nije posebna tabela u bazi).
- Eliminacioni kostur podržava slobodne prolaze (BYE) i izbegava susret ekipa iz iste grupe u prvom kolu.

## Demo alati

Admin na stranici Ekipe može da doda test ekipe, a na stranici turnira da ih prijavi i popuni nasumične rezultate grupne faze i eliminacije. Tako se brzo proveravaju različite konfiguracije (npr. 8 ekipa 2 × 4, 12 ekipa 3 × 4 sa 2 BYE, 10 ekipa u 4 grupe).

## Struktura

- `Controllers/`: API metode (ekipe, turniri, grupe, mečevi, eliminacija, raspored, statistika, prijava, demo)
- `Models/`: klase koje odgovaraju tabelama u bazi
- `Services/`: pravilnik rezultata, računanje tabele, provera termina
- `Data/`: veza sa bazom (Entity Framework)
- `Migrations/`: struktura baze
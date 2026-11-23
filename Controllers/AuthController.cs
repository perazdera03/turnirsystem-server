using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TurnirSystem.Data;
using TurnirSystem.Models;

//kontroler za prijavu i registraciju admina. nema [authorize] jer korisnik nije ni prijavljen
[ApiController]
[Route("api/[controller]")] //ime klase bez Controller = /api/Auth
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context; //pristup bazi
    private readonly IConfiguration _config; //pristup podesavanjima iz appsettings.json (jwt kljuc i slicno)
    private readonly PasswordHasher<Korisnik> _hasher = new(); //alat koji pravi i proverava hash lozinke

    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    //DTO klasa je podatak koji klijent salje (ne ide u bazu)
    public class RegisterRequest //klasa za registraciju
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class LoginRequest //klasa za login
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    //post (salji) metoda. pravi novi nalog admina
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
    {
        bool imaKorisnika = await _context.Korisnici.AnyAsync(); //da li vec postoji bar jedan korisnik
        bool prijavljen = User.Identity?.IsAuthenticated ?? false; //da li je taj koji salje zahtev vec prijavljen

        //prvi nalog moze da napravi svako, ali posle toga samo admin moze napraviti novi nalog
        if (imaKorisnika && !prijavljen)
            return Unauthorized(new { message = "Za pravljenje novog naloga moraš biti prijavljen." }); //greska ako neprijavljen korisnik pokusava da pravi nalog nakon prve registracije

        var username = dto.Username?.Trim(); //brisu se razmaci sa pocetka i kraja

        //uslov if naredba. ime mora imati izmedju 3 i 50 karaktera
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
            return BadRequest(new { message = "Korisničko ime mora imati između 3 i 50 karaktera." });

        //uslov if naredba. lozinka mora imati minimum 6 karaktera
        if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 6)
            return BadRequest(new { message = "Lozinka mora imati bar 6 karaktera." });

        //korisnicko ime mora da bude jedinstveno
        var exists = await _context.Korisnici.AnyAsync(k => k.Username == username);
        if (exists)
            return Conflict(new { message = "Korisničko ime već postoji." }); //ako ime vec postoji, daje gresku 409

        //pravi se novi korisnik, a rola je uvek admin
        var korisnik = new Korisnik { Username = username, Rola = Rola.Admin };
        korisnik.PasswordHash = _hasher.HashPassword(korisnik, dto.Password); //cuva se hash, nikad prava lozinka. lozinka se pretvara u hash koji se ne moze vratiti

        _context.Korisnici.Add(korisnik);
        await _context.SaveChangesAsync(); //tek ovde se sve upisuje u bazu

        return Created("", new { korisnik.KorisnikId, korisnik.Username, korisnik.Rola }); //uspesno kreiranje korisnika
    }

    //post (salji) metoda. prijava admina, ako je lozinka i ime tacno, jwt token se kreira nakon login koji klijent koristi da bi ostao prijavljen na odredjeno vreme
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        var username = dto.Username?.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(dto.Password))
            return BadRequest(new { message = "Unesi korisničko ime i lozinku." }); //poruka ako je prazno polje za ime i lozinku

        var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Username == username); //trazi se korisnik po imenu
        if (korisnik == null)
            return Unauthorized(new { message = "Pogrešni kredencijali." }); //ako neautorizovan korisnik unese lose ime ili lozinku, daje istu gresku da korisnik ne bi saznao sta je pogresno

        var verify = _hasher.VerifyHashedPassword(korisnik, korisnik.PasswordHash, dto.Password); //uporedjuje se hash lozinke sa bazom
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Pogrešni kredencijali." }); //ako je hash netacan


        var token = CreateJwt(korisnik);
        return Ok(new { access_token = token, token_type = "Bearer", username = korisnik.Username });  //ako je tacan hash, korisnik prolazi i dobija jwt token koji se koristi na svaki zahtev klijenta
    }

    //funkcija za jwt token. pravi se i potpisuje se nasim tajnim kljucem
    private string CreateJwt(Korisnik korisnik)
    {
        var jwt = _config.GetSection("Jwt");
        var key = jwt["Key"];
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        var expiryMinutes = int.Parse(jwt["ExpiryMinutes"] ?? "120"); //koliko token vazi. ako nije podesen, onda traje 120 minuta

        //podaci koji su u tokenu: ime, rola i jedinstven id tokena)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, korisnik.Username),
            new Claim(ClaimTypes.Name, korisnik.Username),
            new Claim(ClaimTypes.Role, korisnik.Rola.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        //kljuc za potpis i algoritam HmacSha256
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes), //vreme isteka tokena
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
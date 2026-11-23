using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TurnirSystem.Data;

//pocetak programa. registruju se prvo sve usluge, pa onda se pravi aplikacija. usluge u ovom programu su cors, swagger, baza i jwt
var builder = WebApplication.CreateBuilder(args);

//cors sluzi da vue (klijent) moze da poziva server. inace browser ne bi dozvolio jer su server i klijent u dva razlicita porta
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() //AllowAnyOrigin dozvoljava bilo koju adresu
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//swagger sluzi za testiranje api (dotnet) bez vue. swagger je svoja browser stranica za dotnet
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TurnirSystem API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme //dugme autorizacije u swagger. upisuje se jwt token kako bi se upotrebile zasticene metode
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Unesi token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement //obavezno u swagger da uz svaki hahtev salje token iz authorize dugmeta
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

//adresa baze koja se cita iz appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => //registruje se veza sa bazom. kontroleri sami dobijaju vezu sa bazom
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))); //koristi se mysql, a verziju servera ef sam prepozna

//podesavanja jwt tokena (issuer, audience, key) iz appsettings.json
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"]; //ko izdaje token. u ovom slucaju je nas server
var audience = jwtSection["Audience"]; //kome je namenjen token. u ovom slucaju je nasa aplikacija
var key = jwtSection["Key"]; //tajni kljuc kojim se potpisuje token
var keyBytes = Encoding.UTF8.GetBytes(key!); //tajni kljuc se pretvara u bajtove, potpis tokena trazi ovaj oblik podataka

//podesavanje kako server proverava token od klijenta
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme) //provera ko je prijavljen preko jwt tokena
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, //proverava ko je izdao token i za koga je namenjen
            ValidIssuer = issuer, //token mora imati odredjenog izdavaoca
            ValidateAudience = true, //token mora biti namenjen odredjenoj aplikaciji
            ValidAudience = audience,
            ValidateIssuerSigningKey = true, //proverava potpis tokena nasim kljucem. ako neko izmeni token, potpis nece biti ispravan
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes), //kljuc kojim se proverava potpis tokena
            ValidateLifetime = true, //token prestaje da vazi kad istekne
            ClockSkew = TimeSpan.Zero //nema dodatnog vremena posle isteka tokena
        };
    });

//ovde se ukljucuje [Authorize] i registruju kontroleri
builder.Services.AddAuthorization();
builder.Services.AddControllers();

//od ovde se pravi aplikacija, posle ovoga ne mogu vise da se dodaju usluge
var app = builder.Build();

//swagger radi samo dok razvijamo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//redosled je bitan. prvo https, pa cors, pa prijava (ko si), pa dozvole (sta smes)
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); //povezuje kontrolere sa adresama (/api/...)

app.Run(); //pokrece server
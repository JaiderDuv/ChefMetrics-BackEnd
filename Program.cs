using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

// El documento OpenAPI se serializa con la cultura del proceso. En equipos
// configurados con coma decimal eso produce JSON inválido (por ejemplo,
// "maximum": 999999,99), así que el backend usa una cultura invariante.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// Los appsettings con credenciales están ignorados por seguridad, así que un
// clon nuevo no trae cadena de conexión. En desarrollo se usa LocalDB para que
// cualquier integrante pueda ejecutar el proyecto sin configurar nada.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=ChefMetrics;Trusted_Connection=true;TrustServerCertificate=true;";
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCosteo();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPerfilEmprendimientoRepository, PerfilEmprendimientoRepository>();
builder.Services.AddScoped<IInsumoRepository, InsumoRepository>();
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IRecetaRepository, RecetaRepository>();
builder.Services.AddScoped<IRecetaService, RecetaService>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPerfilEmpreService, PerfilEmpreService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IUmbralMargenRepository, UmbralMargenRepository>();
builder.Services.AddScoped<IEvaluacionMenuService, EvaluacionMenuService>();
builder.Services.AddMemoryCache();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

// Valores por defecto para que un clon sin appsettings pueda iniciar sesión.
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer)) jwtSettings.Issuer = "ChefMetrics";
if (string.IsNullOrWhiteSpace(jwtSettings.Audience)) jwtSettings.Audience = "PWGAPG-Frontend";
if (jwtSettings.AccessTokenMinutes <= 0) jwtSettings.AccessTokenMinutes = 15;
if (jwtSettings.RefreshTokenDays <= 0) jwtSettings.RefreshTokenDays = 7;

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    if (!isDevelopment)
    {
        throw new InvalidOperationException(
            "La configuración Jwt:Key debe existir fuera del repositorio y tener al menos 32 caracteres.");
    }

    // Clave exclusiva de desarrollo. En producción se debe configurar por
    // variables de entorno o user-secrets.
    jwtSettings.Key = "ChefMetrics-Development-Signing-Key-Only-For-Local-Runs-2026";
}

// AuthService y TokenGenerator leen IOptions<JwtSettings>: se aplican los
// mismos valores resueltos que usa la validación de tokens.
builder.Services.PostConfigure<JwtSettings>(options =>
{
    options.Key = jwtSettings.Key;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.AccessTokenMinutes = jwtSettings.AccessTokenMinutes;
    options.RefreshTokenDays = jwtSettings.RefreshTokenDays;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            // Tolerancia mínima para diferencias de reloj entre servidores.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDbContext<ChefMetricsContext>(options =>
    options.UseSqlServer(connectionString));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    allowedOrigins = [
        "http://localhost:5196",
        "https://localhost:7158",
        "https://7fa24239.pwgapg-frontend.pages.dev"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Crea la base en una instalación nueva y aplica migraciones pendientes en
// las bases existentes. Evita que cada integrante deba ejecutar dotnet ef
// (y que la app falle por columnas faltantes).
using (var scope = app.Services.CreateScope())  
{
    var db = scope.ServiceProvider.GetRequiredService<ChefMetricsContext>();
    db.Database.Migrate();
}

// Se expone OpenAPI con acceso anónimo para que no pida token JWT en la especificación
app.MapOpenApi().AllowAnonymous();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "ChefMetrics BackEnd API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Health;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
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
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddDataProtection();
builder.Services.AddScoped<ICredencialProtector, CredencialProtector>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.PostConfigure<EmailSettings>(options =>
{
    if (string.IsNullOrWhiteSpace(options.InvitationUrl))
    {
        options.InvitationUrl = isDevelopment
            ? "https://localhost:7158/aceptar-invitacion"
            : "https://pwgapg-frontend.pages.dev/aceptar-invitacion";
    }
});
builder.Services.AddScoped<IUmbralMargenRepository, UmbralMargenRepository>();
builder.Services.AddScoped<IEvaluacionMenuService, EvaluacionMenuService>();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

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

    options.AddPolicy("SoloEmprendedor", policy => policy.RequireRole("Emprendedor"));
    options.AddPolicy("RecetasLectura", policy => policy.RequireRole("Emprendedor", "Chef"));
    options.AddPolicy("MenuLectura", policy => policy.RequireRole("Emprendedor", "Chef", "Cajero"));
});

builder.Services.AddDbContext<ChefMetricsContext>(options =>
    options.UseSqlServer(connectionString));

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var allowedOrigins = configuredOrigins
    .Concat([
        "http://localhost:5196",
        "https://localhost:7158",
        "https://pwgapg-frontend.pages.dev",
        "https://7fa24239.pwgapg-frontend.pages.dev"
    ])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

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

if (app.Environment.IsDevelopment())
{
    // La política de autorización de respaldo también cubre los endpoints
    // generados por MapOpenApi. El documento solo se expone en Development,
    // pero debe ser anónimo para que Swagger UI pueda cargarlo.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ChefMetrics BackEnd API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var user = context.User;
    var role = user.FindFirst(ClaimTypes.Role)?.Value;
    var tipoCuenta = user.FindFirst("tipoCuenta")?.Value;
    if (user.Identity?.IsAuthenticated == true &&
        string.Equals(tipoCuenta, "empleado", StringComparison.OrdinalIgnoreCase))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var readOnly = HttpMethods.IsGet(context.Request.Method) ||
                       HttpMethods.IsHead(context.Request.Method) ||
                       HttpMethods.IsOptions(context.Request.Method);
        var menuRoute = path.Equals("/api/productos-menu", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/api/productos-menu/", StringComparison.OrdinalIgnoreCase);
        var recipeRoute = path.Equals("/api/recetas", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/api/recetas/", StringComparison.OrdinalIgnoreCase);
        var canReadRoute = menuRoute ||
            (string.Equals(role, "Chef", StringComparison.OrdinalIgnoreCase) && recipeRoute);

        // Reject forbidden methods and routes before opening a database connection.
        if (!readOnly || !canReadRoute)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var cuentaIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        var versionToken = user.FindFirst("versionEmpleado")?.Value;
        if (!int.TryParse(cuentaIdValue, out var cuentaId) || !int.TryParse(versionToken, out var version))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var empleadoActual = await context.RequestServices.GetRequiredService<ChefMetricsContext>()
            .Empleados.AsNoTracking()
            .Where(e => e.CuentaId == cuentaId && e.Activo && e.EstadoInvitacion == "aceptada")
            .Select(e => new { e.SessionVersion, e.NegocioId })
            .SingleOrDefaultAsync();
        if (empleadoActual is null || empleadoActual.SessionVersion != version)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Items[EmpleadoRequestContext.HttpContextItemKey] =
            new EmpleadoRequestContext(cuentaId, empleadoActual.NegocioId);
    }

    await next();
});
app.MapControllers();
app.MapHealthChecks("/health/db").AllowAnonymous();

app.Run();

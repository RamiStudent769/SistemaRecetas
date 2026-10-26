using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using _20263RecetasApi.Auth;
using _20263RecetasApi.Data;
using _20263RecetasApi;


var builder = WebApplication.CreateBuilder(args);

// 1. Registrar servicios generales
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );
builder.Services.AddOpenApi();

// 2. Configuración JWT
// Se enlaza a JwtSettings (la usa JwtService) y se valida al arrancar:
// si falta algo o la Key tiene menos de 32 caracteres, la app no inicia.
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtSettings.Seccion).Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "No se encontró la sección 'Jwt' en la configuración.");

if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key debe tener al menos 32 caracteres. " +
        "Configúrala con: dotnet user-secrets set \"Jwt:Key\" \"<clave>\"");

// 3. Registrar autenticación JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Key)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

        // Un usuario desactivado o eliminado deja de entrar de inmediato,
        // aunque su token todavía no haya expirado.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<Usuario>>();

                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var usuario = id == null ? null : await userManager.FindByIdAsync(id);

                if (usuario == null || !usuario.Activo)
                {
                    context.Fail("El usuario no existe o está desactivado.");
                    return;
                }

                var rolesEnToken = context.Principal?
                    .FindAll(ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
                var rolesActuales = await userManager.GetRolesAsync(usuario);

                if (rolesEnToken.Except(
                        rolesActuales,
                        StringComparer.OrdinalIgnoreCase)
                    .Any())
                {
                    context.Fail("Los permisos del usuario cambiaron. Inicia sesión nuevamente.");
                }
            }
        };
    });


// 4. Registrar autorización
// Por defecto TODO endpoint exige token. Solo lo marcado con
// [AllowAnonymous] (el login) queda público.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services
    .AddIdentityCore<Usuario>(options =>
    {
        // Configuración básica de contraseñas
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Configuración del usuario
        options.User.RequireUniqueEmail = true;

        // Bloqueo tras varios intentos fallidos
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// Servicios de la aplicación

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 5. Construir la aplicación
var app = builder.Build();

// En desarrollo, crea el esquema pendiente (incluidas las tablas de Identity)
// antes de que el seeder consulte los roles.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Crea los roles y el administrador inicial.
await IdentitySeeder.SembrarAsync(app.Services);

// 6. Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    // Con la política por defecto, el documento OpenAPI también pediría token
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // siempre antes de UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();

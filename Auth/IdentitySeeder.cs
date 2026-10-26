using Microsoft.AspNetCore.Identity;
using _20263RecetasApi.Models;

namespace _20263RecetasApi.Auth
{
    // Crea los roles y, si todavía no existe ningún Admin, el administrador inicial.
    // Credenciales leidas de configuración (User Secrets), nunca escritas en el código:
    //   SeedAdmin:Correo, SeedAdmin:Password (y opcional SeedAdmin:Nombre, SeedAdmin:Apellidos)
    public static class IdentitySeeder
    {
        public static async Task SembrarAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<Usuario>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger("IdentitySeeder");

            foreach (var rol in Roles.Todos)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            // Si ya hay un administrador, no se hace nada más
            if ((await userManager.GetUsersInRoleAsync(Roles.Admin)).Count > 0)
                return;

            var correo = config["SeedAdmin:Correo"];
            var password = config["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "No hay ningún Admin y falta SeedAdmin:Correo / SeedAdmin:Password " +
                    "en la configuración. No se creó el administrador inicial.");
                return;
            }

            var admin = await userManager.FindByEmailAsync(correo);

            if (admin == null)
            {
                admin = new Usuario
                {
                    UserName = correo,
                    Email = correo,
                    EmailConfirmed = true,
                    Nombre = config["SeedAdmin:Nombre"] ?? "Administrador",
                    Apellido = config["SeedAdmin:Apellidos"] ?? "Sistema",
                    Activo = true
                };

                var crear = await userManager.CreateAsync(admin, password);

                if (!crear.Succeeded)
                {
                    logger.LogError(
                        "No se pudo crear el administrador inicial: {Errores}",
                        string.Join("; ", crear.Errors.Select(e => e.Description)));
                    return;
                }
            }

            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation(
                "Administrador inicial listo: {Correo}", correo);
        }
    }
}

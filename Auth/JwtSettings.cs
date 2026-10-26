using System.ComponentModel.DataAnnotations;
namespace _20263RecetasApi.Auth
{
    // Se enlaza con la sección "Jwt" de la configuración.
    // La Key NO va en appsettings.json: se guarda en User Secrets (desarrollo)
    // o en variables de entorno (producción).
    public class JwtSettings
    {
        public const string Seccion = "Jwt";

        // Mínimo 32 caracteres (256 bits) para HMAC-SHA256
        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpirationMinutes { get; set; } = 30;
    }
}

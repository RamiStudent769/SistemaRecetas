using System.ComponentModel.DataAnnotations;

namespace _20263RecetasApi.DTO
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRespuestaDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraUtc { get; set; }
        public required UsuarioRespuestaDto Usuario { get; set; }
    }

    public class CambiarPasswordDto
    {
        [Required]
        [MaxLength(100)]
        public string PasswordActual { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PasswordNueva { get; set; } = string.Empty;
    }
}

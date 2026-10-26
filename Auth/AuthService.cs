using Microsoft.AspNetCore.Identity;
using _20263RecetasApi.DTO;
using _20263RecetasApi.Models;

namespace _20263RecetasApi.Auth
{
    public enum ResultadoAuth
    {
        Ok,
        CredencialesInvalidas,
        CuentaBloqueada,
        CuentaInactiva
    }

    public record LoginResultado(
        ResultadoAuth Resultado,
        LoginRespuestaDto? Datos = null);

    public interface IAuthService
    {
        Task<LoginResultado> LoginAsync(LoginDto dto);
        Task<UsuarioRespuestaDto?> ObtenerPerfilAsync(string usuarioId);
        Task<IdentityResult> CambiarPasswordAsync(
            string usuarioId, CambiarPasswordDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IJwtService _jwtService;

        public AuthService(
            UserManager<Usuario> userManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<LoginResultado> LoginAsync(LoginDto dto)
        {
            var usuario = await _userManager.FindByEmailAsync(dto.Correo.Trim());

            // Mismo resultado para "no existe" y "contraseña incorrecta":
            // así no se revela qué correos están registrados.
            if (usuario == null)
                return new(ResultadoAuth.CredencialesInvalidas);

            if (await _userManager.IsLockedOutAsync(usuario))
                return new(ResultadoAuth.CuentaBloqueada);

            if (!await _userManager.CheckPasswordAsync(usuario, dto.Password))
            {
                // Suma un intento fallido; al llegar al máximo bloquea la cuenta
                await _userManager.AccessFailedAsync(usuario);

                return await _userManager.IsLockedOutAsync(usuario)
                    ? new(ResultadoAuth.CuentaBloqueada)
                    : new(ResultadoAuth.CredencialesInvalidas);
            }

            // Se revisa DESPUÉS de validar la contraseña para no confirmar
            // a un extraño que la cuenta existe pero está desactivada.
            if (!usuario.Activo)
                return new(ResultadoAuth.CuentaInactiva);

            await _userManager.ResetAccessFailedCountAsync(usuario);

            var roles = await _userManager.GetRolesAsync(usuario);
            var (token, expiraUtc) = _jwtService.GenerarToken(usuario, roles);

            return new(ResultadoAuth.Ok, new LoginRespuestaDto
            {
                Token = token,
                ExpiraUtc = expiraUtc,
                Usuario = UsuarioRespuestaDto.Desde(usuario, roles)
            });
        }

        public async Task<UsuarioRespuestaDto?> ObtenerPerfilAsync(
            string usuarioId)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);

            if (usuario == null || !usuario.Activo)
                return null;

            var roles = await _userManager.GetRolesAsync(usuario);
            return UsuarioRespuestaDto.Desde(usuario, roles);
        }

        public async Task<IdentityResult> CambiarPasswordAsync(
            string usuarioId, CambiarPasswordDto dto)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);

            if (usuario == null || !usuario.Activo)
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Usuario no encontrado."
                });

            return await _userManager.ChangePasswordAsync(
                usuario, dto.PasswordActual, dto.PasswordNueva);
        }
    }
}

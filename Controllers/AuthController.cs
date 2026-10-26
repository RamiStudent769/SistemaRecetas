using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _20263RecetasApi.Auth;
using _20263RecetasApi.DTO;

namespace _20263RecetasApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/login
        // Único endpoint público de toda la API (el resto exige token).
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginRespuestaDto>> Login([FromBody]LoginDto dto)
        {

            var resultado = await _authService.LoginAsync(dto);

            switch (resultado.Resultado)
            {
                case ResultadoAuth.Ok:
                    return Ok(resultado.Datos);

                case ResultadoAuth.CuentaBloqueada:
                    return StatusCode(StatusCodes.Status423Locked, new
                    {
                        mensaje = "La cuenta está bloqueada temporalmente por demasiados intentos fallidos. Intenta de nuevo más tarde."
                    });

                case ResultadoAuth.CuentaInactiva:
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        mensaje = "Tu cuenta está desactivada. Contacta al administrador."
                    });

                default:
                    return Unauthorized(new
                    {
                        mensaje = "Correo o contraseña incorrectos."
                    });
            }
        }

        // GET api/auth/me  -> datos y roles del usuario del token
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UsuarioRespuestaDto>> Perfil()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id == null)
                return Unauthorized();

            var perfil = await _authService.ObtenerPerfilAsync(id);

            if (perfil == null)
                return Unauthorized();

            return Ok(perfil);
        }

        // POST api/auth/cambiar-password
        //Debe estar validado con el token del usuario que quiere cambiar su propia contraseña. No es para que el Admin cambie la contraseña de otro usuario.
        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordDto dto)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id == null)
                return Unauthorized();

            var resultado = await _authService.CambiarPasswordAsync(id, dto);

            if (!resultado.Succeeded)
                return BadRequest(new
                {
                    mensaje = "No se pudo cambiar la contraseña.",
                    errores = resultado.Errors.Select(e => e.Description)
                });

//                 {
    
//     "id":"ce37e106-30d9-49da-8d73-459d30a42282",
//     "passwordNueva":"1Ab23456789@",
//     "passwordActual":"1Ab2345678@"


// }

            return NoContent();
        }
    }
}

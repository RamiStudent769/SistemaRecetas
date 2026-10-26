using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _20263RecetasApi.Auth;
using _20263RecetasApi.DTO;
using _20263RecetasApi;

namespace _20263RecetasApi.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public sealed class UsuariosController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        UserManager<Usuario> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsuariosController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    [EndpointSummary("Lista los usuarios registrados, incluidos los desactivados.")]
    [ProducesResponseType(typeof(IEnumerable<UsuarioRespuestaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UsuarioRespuestaDto>>> ObtenerTodos(
        CancellationToken cancellationToken)
    {
        var usuarios = await _userManager.Users
            .AsNoTracking()
            .OrderBy(usuario => usuario.Apellido)
            .ThenBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);

        var respuestas = new List<UsuarioRespuestaDto>(usuarios.Count);
        foreach (var usuario in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(usuario);
            respuestas.Add(UsuarioRespuestaDto.Desde(usuario, roles));
        }

        return Ok(respuestas);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Obtiene un usuario por su identificador.")]
    [ProducesResponseType(typeof(UsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioRespuestaDto>> ObtenerPorId(
        string id,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

        if (usuario is null)
            return UsuarioNoEncontrado();

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(UsuarioRespuestaDto.Desde(usuario, roles));
    }

    [HttpPost]
    [EndpointSummary("Crea un usuario y le asigna un rol.")]
    [ProducesResponseType(typeof(UsuarioRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioRespuestaDto>> Crear(
        UsuarioCrearDto dto,
        CancellationToken cancellationToken)
    {
        var rol = dto.Rol.Trim();
        if (!await _roleManager.RoleExistsAsync(rol))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "El rol indicado no existe."
            });

        var correo = dto.Correo.Trim();
        var usuario = new Usuario
        {
            UserName = correo,
            Email = correo,
            Nombre = dto.Nombre.Trim(),
            Apellido= dto.Apellidos.Trim(),
            Activo = true
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
            return ErrorCreacion(resultado);

        resultado = await _userManager.AddToRoleAsync(usuario, rol);
        if (!resultado.Succeeded)
        {
            var eliminacion = await _userManager.DeleteAsync(usuario);
            if (!eliminacion.Succeeded)
            {
                _logger.LogError(
                    "No se pudo eliminar el usuario {UsuarioId} después de fallar la asignación del rol: {Errores}",
                    usuario.Id,
                    string.Join("; ", eliminacion.Errors.Select(error => error.Description)));

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "No se pudo completar la creación del usuario.");
            }

            return BadRequest(new ValidationProblemDetails(
                resultado.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.Select(error => error.Description).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No se pudo asignar el rol al usuario."
            });
        }

        var respuesta = UsuarioRespuestaDto.Desde(usuario, new[] { rol });
        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = usuario.Id },
            respuesta);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Actualiza los datos personales y el correo de un usuario.")]
    [ProducesResponseType(typeof(UsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioRespuestaDto>> Actualizar(
        string id,
        UsuarioActualizarDto dto,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
            return UsuarioNoEncontrado();

        var correo = dto.Correo.Trim();
        if (!string.Equals(usuario.Email, correo, StringComparison.OrdinalIgnoreCase))
        {
            usuario.EmailConfirmed = false;
            usuario.Email = correo;
            usuario.UserName = correo;
        }

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellidos.Trim();

        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return ErrorActualizacion(resultado);

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(UsuarioRespuestaDto.Desde(usuario, roles));
    }

    [HttpPut("{id}/rol")]
    [EndpointSummary("Reemplaza los roles de un usuario por el rol indicado.")]
    [ProducesResponseType(typeof(UsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioRespuestaDto>> CambiarRol(
        string id,
        UsuarioCambiarRolDto dto,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
            return UsuarioNoEncontrado();

        var nuevoRol = dto.Rol.Trim();
        if (!await _roleManager.RoleExistsAsync(nuevoRol))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "El rol indicado no existe."
            });

        var rolesActuales = await _userManager.GetRolesAsync(usuario);
        if (usuario.Activo &&
            rolesActuales.Contains(Roles.Admin) &&
            !string.Equals(nuevoRol, Roles.Admin, StringComparison.OrdinalIgnoreCase) &&
            await EsUltimoAdministradorActivoAsync(usuario))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se puede quitar el rol Admin al último administrador activo."
            });
        }

        var rolesAEliminar = rolesActuales
            .Where(rol => !string.Equals(rol, nuevoRol, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var asignacionNueva = !rolesActuales.Contains(
            nuevoRol,
            StringComparer.OrdinalIgnoreCase);

        if (asignacionNueva)
        {
            var asignacion = await _userManager.AddToRoleAsync(usuario, nuevoRol);
            if (!asignacion.Succeeded)
                return ErrorIdentidad(asignacion, "No se pudo asignar el rol al usuario.");
        }

        if (rolesAEliminar.Length > 0)
        {
            var eliminacion = await _userManager.RemoveFromRolesAsync(usuario, rolesAEliminar);
            if (!eliminacion.Succeeded)
            {
                if (asignacionNueva)
                {
                    var rollback = await _userManager.RemoveFromRoleAsync(usuario, nuevoRol);
                    if (!rollback.Succeeded)
                    {
                        _logger.LogError(
                            "No se pudo revertir la asignación del rol {Rol} al usuario {UsuarioId}: {Errores}",
                            nuevoRol,
                            usuario.Id,
                            string.Join("; ", rollback.Errors.Select(error => error.Description)));
                    }
                }

                return ErrorIdentidad(eliminacion, "No se pudieron reemplazar los roles del usuario.");
            }
        }

        return Ok(UsuarioRespuestaDto.Desde(usuario, new[] { nuevoRol }));
    }

    [HttpPut("{id}/restablecer-password")]
    [EndpointSummary("Restablece la contraseña de un usuario.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestablecerPassword(
        string id,
        RestablecerPasswordDto dto,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
            return UsuarioNoEncontrado();

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await _userManager.ResetPasswordAsync(
            usuario,
            token,
            dto.PasswordNueva);

        return resultado.Succeeded
            ? NoContent()
            : ErrorIdentidad(resultado, "No se pudo restablecer la contraseña.");
    }

    [HttpPatch("{id}/estado")]
    [EndpointSummary("Activa o desactiva una cuenta de usuario.")]
    [ProducesResponseType(typeof(UsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioRespuestaDto>> CambiarEstado(
        string id,
        UsuarioCambiarEstadoDto dto,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
            return UsuarioNoEncontrado();

        if (usuario.Activo &&
            dto.Activo == false &&
            EsUsuarioActual(usuario))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No puedes desactivar tu propia cuenta."
            });
        }

        if (usuario.Activo &&
            dto.Activo == false &&
            await _userManager.IsInRoleAsync(usuario, Roles.Admin) &&
            await EsUltimoAdministradorActivoAsync(usuario))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se puede desactivar al último administrador activo."
            });
        }

        usuario.Activo = dto.Activo!.Value;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return ErrorIdentidad(resultado, "No se pudo cambiar el estado del usuario.");

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(UsuarioRespuestaDto.Desde(usuario, roles));
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Desactiva la cuenta de usuario sin eliminar sus datos.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(
        string id,
        CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
            return UsuarioNoEncontrado();

        if (EsUsuarioActual(usuario))
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No puedes desactivar tu propia cuenta."
            });

        if (usuario.Activo &&
            await _userManager.IsInRoleAsync(usuario, Roles.Admin) &&
            await EsUltimoAdministradorActivoAsync(usuario))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se puede desactivar al último administrador activo."
            });
        }

        usuario.Activo = false;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return ErrorIdentidad(resultado, "No se pudo desactivar el usuario.");

        return NoContent();
    }

    private async Task<bool> EsUltimoAdministradorActivoAsync(Usuario usuario)
    {
        var administradores = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        return administradores.Count(administrador =>
            administrador.Activo && administrador.Id != usuario.Id) == 0;
    }

    private bool EsUsuarioActual(Usuario usuario) =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) == usuario.Id;

    private NotFoundObjectResult UsuarioNoEncontrado() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Usuario no encontrado."
        });

    private ActionResult<UsuarioRespuestaDto> ErrorCreacion(IdentityResult resultado)
    {
        if (resultado.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ya existe un usuario con ese correo."
            });
        }

        return ErrorIdentidad(resultado, "No se pudo crear el usuario.");
    }

    private ActionResult<UsuarioRespuestaDto> ErrorActualizacion(
        IdentityResult resultado)
    {
        if (resultado.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ya existe un usuario con ese correo."
            });
        }

        return ErrorIdentidad(resultado, "No se pudieron actualizar los datos del usuario.");
    }

    private static BadRequestObjectResult ErrorIdentidad(
        IdentityResult resultado,
        string titulo) =>
        new(new ValidationProblemDetails(
            resultado.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Select(error => error.Description).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = titulo
        });
}

using System.ComponentModel.DataAnnotations;
using _20263RecetasApi;

namespace _20263RecetasApi.DTO;

/// <summary>Datos requeridos para crear un usuario.</summary>
public sealed record UsuarioCrearDto
{
    [Required, MaxLength(80)]
    public required string Nombre { get; init; }

    [Required, MaxLength(100)]
    public required string Apellidos { get; init; }

    [Required, EmailAddress, MaxLength(256)]
    public required string Correo { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public required string Password { get; init; }

    [Required]
    public required string Rol { get; init; }
}

/// <summary>Datos para actualizar el nombre, los apellidos y el correo del usuario.</summary>
public sealed record UsuarioActualizarDto
{
    [Required, MaxLength(80)]
    public required string Nombre { get; init; }

    [Required, MaxLength(100)]
    public required string Apellidos { get; init; }

    [Required, EmailAddress, MaxLength(256)]
    public required string Correo { get; init; }
}

/// <summary>Datos para asignar el rol de un usuario.</summary>
public sealed record UsuarioCambiarRolDto
{
    [Required]
    public required string Rol { get; init; }
}

/// <summary>Datos para cambiar el estado activo de un usuario.</summary>
public sealed record UsuarioCambiarEstadoDto
{
    [Required]
    public required bool? Activo { get; init; }
}

/// <summary>Datos para restablecer la contraseña de un usuario.</summary>
public sealed record RestablecerPasswordDto
{
    [Required, MinLength(8), MaxLength(100)]
    public required string PasswordNueva { get; init; }
}

/// <summary>Usuario devuelto por la API, junto con sus roles.</summary>
public sealed record UsuarioRespuestaDto(
    string Id,
    string Nombre,
    string Apellidos,
    string Correo,
    bool Activo,
    DateTimeOffset FechaCreacion,
    IReadOnlyList<string> Roles)
{
    public static UsuarioRespuestaDto Desde(
        Usuario usuario,
        IEnumerable<string> roles) =>
        new(
            usuario.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email ?? string.Empty,
            usuario.Activo,
            usuario.FechaCreacion,
            roles.ToArray());
}

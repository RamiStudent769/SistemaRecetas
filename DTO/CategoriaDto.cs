using System.ComponentModel.DataAnnotations;
using _20263RecetasApi.Models;

namespace _20263RecetasApi.DTO;

/// <summary>Datos para crear una categoría de recetas.</summary>
public sealed record CategoriaCrearDto
{
    [Required]
    public required string Nombre { get; init; }

    public string? Descripcion { get; init; }
}

/// <summary>Datos para reemplazar los campos de una categoría.</summary>
public sealed record CategoriaActualizarDto
{
    [Required]
    public required string Nombre { get; init; }

    public string? Descripcion { get; init; }
}

/// <summary>Categoría devuelta por la API.</summary>
public sealed record CategoriaResponseDto(int Id, string Nombre, string? Descripcion)
{
    public static CategoriaResponseDto Desde(Categoria categoria) =>
        new(categoria.id, categoria.Nombre, categoria.Descripcion);
}

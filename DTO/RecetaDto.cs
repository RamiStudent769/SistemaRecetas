using System.ComponentModel.DataAnnotations;

namespace _20263RecetasApi.DTO;

/// <summary>Datos para crear una receta.</summary>
public sealed record RecetaCrearDto
{
    [Required]
    public required string Nombre { get; init; }

    public string? Descripcion { get; init; }

    [Required]
    public required string Ingredientes { get; init; }

    [Required]
    public required string Preparacion { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; init; }
}

/// <summary>Datos para reemplazar los campos de una receta.</summary>
public sealed record RecetaActualizarDto
{
    [Required]
    public required string Nombre { get; init; }

    public string? Descripcion { get; init; }

    [Required]
    public required string Ingredientes { get; init; }

    [Required]
    public required string Preparacion { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; init; }
}

/// <summary>Receta devuelta por la API, incluida su categoría.</summary>
public sealed record RecetaResponseDto(
    int Id,
    string Nombre,
    string? Descripcion,
    string Ingredientes,
    string Preparacion,
    int CategoriaId,
    CategoriaResponseDto? Categoria);

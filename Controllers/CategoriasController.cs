using _20263RecetasApi.Data;
using _20263RecetasApi.DTO;
using _20263RecetasApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _20263RecetasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [EndpointSummary("Lista las categorías de recetas.")]
    [ProducesResponseType(typeof(IEnumerable<CategoriaResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoriaResponseDto>>> ObtenerTodos(
        CancellationToken cancellationToken)
    {
        var categorias = await _context.Categorias
            .AsNoTracking()
            .OrderBy(categoria => categoria.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(categorias.Select(CategoriaResponseDto.Desde));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obtiene una categoría por su identificador.")]
    [ProducesResponseType(typeof(CategoriaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaResponseDto>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.id == id, cancellationToken);

        return categoria is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada."
            })
            : Ok(CategoriaResponseDto.Desde(categoria));
    }

    [HttpPost]
    [EndpointSummary("Crea una categoría.")]
    [ProducesResponseType(typeof(CategoriaResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoriaResponseDto>> Crear(
        CategoriaCrearDto dto,
        CancellationToken cancellationToken)
    {
        var categoria = new Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim()
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync(cancellationToken);

        var respuesta = CategoriaResponseDto.Desde(categoria);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = categoria.id }, respuesta);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Actualiza todos los campos de una categoría.")]
    [ProducesResponseType(typeof(CategoriaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaResponseDto>> Actualizar(
        int id,
        CategoriaActualizarDto dto,
        CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(item => item.id == id, cancellationToken);

        if (categoria is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada."
            });

        categoria.Nombre = dto.Nombre.Trim();
        categoria.Descripcion = dto.Descripcion?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(CategoriaResponseDto.Desde(categoria));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Elimina una categoría que no tenga recetas asociadas.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(
        int id,
        CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(item => item.id == id, cancellationToken);

        if (categoria is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada."
            });

        var tieneRecetas = await _context.Recetas
            .AnyAsync(receta => receta.CategoriaId == id, cancellationToken);

        if (tieneRecetas)
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No se puede eliminar la categoría porque tiene recetas asociadas."
            });

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

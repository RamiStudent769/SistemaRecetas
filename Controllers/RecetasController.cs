using _20263RecetasApi.Data;
using _20263RecetasApi.DTO;
using _20263RecetasApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _20263RecetasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RecetasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecetasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [EndpointSummary("Lista las recetas y permite filtrarlas por categoría.")]
    [ProducesResponseType(typeof(IEnumerable<RecetaResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecetaResponseDto>>> ObtenerTodos(
        [FromQuery] int? categoriaId,
        CancellationToken cancellationToken)
    {
        var consulta = _context.Recetas
            .AsNoTracking()
            .Include(receta => receta.Categoria)
            .AsQueryable();

        if (categoriaId.HasValue)
            consulta = consulta.Where(receta => receta.CategoriaId == categoriaId.Value);

        var recetas = await consulta
            .OrderBy(receta => receta.Nombre)
            .ToListAsync(cancellationToken);

        return Ok(recetas.Select(Desde));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obtiene una receta por su identificador.")]
    [ProducesResponseType(typeof(RecetaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecetaResponseDto>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var receta = await _context.Recetas
            .AsNoTracking()
            .Include(item => item.Categoria)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return receta is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Receta no encontrada."
            })
            : Ok(Desde(receta));
    }

    [HttpPost]
    [EndpointSummary("Crea una receta.")]
    [ProducesResponseType(typeof(RecetaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecetaResponseDto>> Crear(
        RecetaCrearDto dto,
        CancellationToken cancellationToken)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.id == dto.CategoriaId, cancellationToken);

        if (categoria is null)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La categoría indicada no existe."
            });

        var receta = new Receta
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Ingredientes = dto.Ingredientes.Trim(),
            Preparacion = dto.Preparacion.Trim(),
            CategoriaId = dto.CategoriaId
        };

        _context.Recetas.Add(receta);
        await _context.SaveChangesAsync(cancellationToken);

        receta.Categoria = categoria;
        var respuesta = Desde(receta);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = receta.Id }, respuesta);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Actualiza todos los campos de una receta.")]
    [ProducesResponseType(typeof(RecetaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecetaResponseDto>> Actualizar(
        int id,
        RecetaActualizarDto dto,
        CancellationToken cancellationToken)
    {
        var receta = await _context.Recetas
            .Include(item => item.Categoria)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (receta is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Receta no encontrada."
            });

        var categoria = await _context.Categorias
            .SingleOrDefaultAsync(item => item.id == dto.CategoriaId, cancellationToken);

        if (categoria is null)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La categoría indicada no existe."
            });

        receta.Nombre = dto.Nombre.Trim();
        receta.Descripcion = dto.Descripcion?.Trim();
        receta.Ingredientes = dto.Ingredientes.Trim();
        receta.Preparacion = dto.Preparacion.Trim();
        receta.CategoriaId = dto.CategoriaId;
        receta.Categoria = categoria;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Desde(receta));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Elimina una receta.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        int id,
        CancellationToken cancellationToken)
    {
        var receta = await _context.Recetas
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (receta is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Receta no encontrada."
            });

        _context.Recetas.Remove(receta);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static RecetaResponseDto Desde(Receta receta) =>
        new(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.Ingredientes,
            receta.Preparacion,
            receta.CategoriaId,
            receta.Categoria is null
                ? null
                : CategoriaResponseDto.Desde(receta.Categoria));
}

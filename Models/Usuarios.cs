using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace _20263RecetasApi
{
    public class Usuario : IdentityUser{
        [Required]
        [MaxLength (80)]
        public string Nombre{get;  set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido {get;  set;} = string.Empty;

        public bool Activo {get;  set;} = true;

        public DateTime FechaCreacion {get;  set; } = DateTime.Now;
        
    }
}
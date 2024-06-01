using System.ComponentModel.DataAnnotations;

namespace ProyectoEncuesta.Models
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public required string Nombre { get; set; }
        public required string Contraseña { get; set; }
    }
}

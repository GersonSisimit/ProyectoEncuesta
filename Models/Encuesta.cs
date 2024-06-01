namespace ProyectoEncuesta.Models
{
    public class Encuesta
    {
        public int IDEncuesta { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public  string IdAux { get; set; }

        public List<CampoEncuesta>  Campos { get; set; }
    }
}

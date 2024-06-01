namespace ProyectoEncuesta.Models
{
    public class CampoEncuesta
    {
        public int IDCampoEncuesta { get; set; }
        public required int IDEncuesta { get; set; }
        public required string NombreCampo { get; set; }
        public required string Titulo { get; set; }
        public required string Descripcion { get; set; }
        public required string TipoInput { get; set; }
        public required bool Requerido { get; set; }
    }
}

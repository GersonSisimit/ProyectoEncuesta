namespace ProyectoEncuesta.Models
{
    public class RespuestaEncuesta
    {
        public int IDRespuestaEncuesta { get; set; }
        public required int IDCampoEncuesta { get; set; }
        public required string Respuesta { get; set; }
        public  DateTime FechaRespuesta { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoEncuesta.Models;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ProyectoEncuesta.Controllers
{
    [Authorize]
    public class EncuestaController : Controller
    {
        #region Configuraciones
        private readonly IConfiguration _configuration;

        private static readonly string key = "DevelPassword"; // Debe ser una clave segura
        private static readonly byte[] salt = new byte[] { 0x43, 0x87, 0x23, 0x72, 0x20, 0x11, 0x17, 0x91 };

        public static string Encriptar(int clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText.ToString());
            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(key, salt);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    return BitConverter.ToString(ms.ToArray()).Replace("-", "");
                }
            }
        }

        public static int Desencriptar(string cipherText)
        {
            byte[] cipherBytes = new byte[cipherText.Length / 2];
            for (int i = 0; i < cipherText.Length; i += 2)
            {
                cipherBytes[i / 2] = Convert.ToByte(cipherText.Substring(i, 2), 16);
            }

            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(key, salt);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    return int.Parse(Encoding.Unicode.GetString(ms.ToArray()));
                }
            }
        }
        public EncuestaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        #endregion

        #region Crear encuesta
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CrearEncuesta(Encuesta model)
        {
            int IdCreado;
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_InsertEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nombre", model.Nombre);

                // Verificar si la descripción es nula y enviar un espacio en blanco en su lugar
                string descripcion = string.IsNullOrEmpty(model.Descripcion) ? " " : model.Descripcion;
                cmd.Parameters.AddWithValue("@Descripcion", descripcion);

                conn.Open();

                // Ejecutar el comando y capturar el ID generado
                IdCreado = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return RedirectToAction("AgregarCampoEncuesta", new { Id = Encriptar(IdCreado) });
        }
        public IActionResult AgregarCampoEncuesta(string Id)
        {
            int IDEncuesta = 0;
            try
            {
                IDEncuesta = Desencriptar(Id);
            }
            catch (Exception)
            {
                return StatusCode(404, "Not found");
            }

            Encuesta encuesta = null;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        encuesta = new Encuesta
                        {
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"].ToString()
                        };
                    }
                }
            }
            if (encuesta == null)
            {
                return StatusCode(404, "Not found");
            }
            List<CampoEncuesta> campos = new List<CampoEncuesta>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectCamposPorEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CampoEncuesta campo = new CampoEncuesta
                        {
                            IDCampoEncuesta = Convert.ToInt32(reader["IDCampoEncuesta"]),
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            NombreCampo = reader["NombreCampo"].ToString(),
                            Titulo = reader["Titulo"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            TipoInput = reader["TipoInput"].ToString(),
                            Requerido = Convert.ToBoolean(reader["Requerido"])
                        };
                        campos.Add(campo);
                    }
                }
            }
            encuesta.Campos = campos;
            encuesta.IdAux = Id;
            return View(encuesta);
        }

        public IActionResult CrearCampoEncuesta(CampoEncuesta model)
        {
            int newId;
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_InsertCampoEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", model.IDEncuesta);
                cmd.Parameters.AddWithValue("@NombreCampo", model.NombreCampo);
                cmd.Parameters.AddWithValue("@Titulo", model.Titulo);
                cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(model.Descripcion) ? " " : model.Descripcion);
                cmd.Parameters.AddWithValue("@TipoInput", model.TipoInput);
                cmd.Parameters.AddWithValue("@Requerido", model.Requerido);

                conn.Open();
                newId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return RedirectToAction("AgregarCampoEncuesta", new { Id = Encriptar(model.IDEncuesta) });
        }

        #endregion

        #region Administrar encuestas

        public IActionResult Administrar()
        {
            List<Encuesta> encuestas = new List<Encuesta>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("GetAllEncuestas", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Encuesta encuesta = new Encuesta
                            {
                                IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                                Nombre = reader["Nombre"].ToString(),
                                Descripcion = reader["Descripcion"].ToString(),
                                IdAux = Encriptar(Convert.ToInt32(reader["IDEncuesta"]))
                            };
                            encuestas.Add(encuesta);
                        }
                    }
                }
            }

            return View(encuestas);
        }

        [HttpPost]
        public IActionResult EliminarEncuesta(int Id)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("DeleteEncuesta", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IDEncuesta", Id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Administrar");
        }


        public IActionResult EditarEncuesta(string Id)
        {
            int IDEncuesta = 0;
            try
            {
                IDEncuesta = Desencriptar(Id);
            }
            catch (Exception)
            {
                return StatusCode(404, "Not found");
            }

            Encuesta encuesta = null;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        encuesta = new Encuesta
                        {
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"].ToString()
                        };
                    }
                }
            }
            if (encuesta == null)
            {
                return StatusCode(404, "Not found");
            }
            List<CampoEncuesta> campos = new List<CampoEncuesta>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectCamposPorEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CampoEncuesta campo = new CampoEncuesta
                        {
                            IDCampoEncuesta = Convert.ToInt32(reader["IDCampoEncuesta"]),
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            NombreCampo = reader["NombreCampo"].ToString(),
                            Titulo = reader["Titulo"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            TipoInput = reader["TipoInput"].ToString(),
                            Requerido = Convert.ToBoolean(reader["Requerido"])
                        };
                        campos.Add(campo);
                    }
                }
            }
            encuesta.Campos = campos;
            encuesta.IdAux = Id;

            return View(encuesta);
        }


        [HttpPost]
        public IActionResult EditarEncuesta(Encuesta encuesta)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("EditEncuesta", connection))
                {
                    if (encuesta.Descripcion == null)
                    {
                        encuesta.Descripcion = "";
                    }
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IDEncuesta", encuesta.IDEncuesta);
                    command.Parameters.AddWithValue("@Nombre", encuesta.Nombre);
                    command.Parameters.AddWithValue("@Descripcion", encuesta.Descripcion);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("EditarEncuesta", new { Id = Encriptar(encuesta.IDEncuesta) });

        }


        [HttpPost]
        public IActionResult EditarCampoEncuesta(CampoEncuesta campo)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("EditCampoEncuesta", connection))
                {
                    if (campo.Descripcion == null)
                    {
                        campo.Descripcion = "";
                    }
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IDCampoEncuesta", campo.IDCampoEncuesta);
                    command.Parameters.AddWithValue("@NombreCampo", campo.NombreCampo);
                    command.Parameters.AddWithValue("@Titulo", campo.Titulo);
                    command.Parameters.AddWithValue("@Descripcion", campo.Descripcion);
                    command.Parameters.AddWithValue("@TipoInput", campo.TipoInput);
                    command.Parameters.AddWithValue("@Requerido", campo.Requerido);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("EditarEncuesta", new { Id = Encriptar(campo.IDEncuesta) });
        }

        [HttpPost]
        public IActionResult EliminarCampoEncuesta(int id)
        {
            string refererUrl = Request.Headers["Referer"].ToString();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("DeleteCampoEncuesta", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IDCampoEncuesta", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            return RedirectToAction("Administrar"); // Fallback action if referer is null or empty
        }


        public IActionResult VerRespuestas(string Id)
        {
            int IDEncuesta = 0;
            try
            {
                IDEncuesta = Desencriptar(Id);
            }
            catch (Exception)
            {
                return StatusCode(404, "Not found");
            }

            Encuesta encuesta = null;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        encuesta = new Encuesta
                        {
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"].ToString()
                        };
                    }
                }
            }
            if (encuesta == null)
            {
                return StatusCode(404, "Not found");
            }
            List<CampoEncuesta> campos = new List<CampoEncuesta>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectCamposPorEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IDEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CampoEncuesta campo = new CampoEncuesta
                        {
                            IDCampoEncuesta = Convert.ToInt32(reader["IDCampoEncuesta"]),
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            NombreCampo = reader["NombreCampo"].ToString(),
                            Titulo = reader["Titulo"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            TipoInput = reader["TipoInput"].ToString(),
                            Requerido = Convert.ToBoolean(reader["Requerido"]),
                            Resuestas = new List<RespuestaEncuesta>()
                        };

                        using (SqlConnection conn2 = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                        {
                            SqlCommand cmd2 = new SqlCommand("GetRespuestasPorCampo", conn2);
                            cmd2.CommandType = CommandType.StoredProcedure;
                            cmd2.Parameters.AddWithValue("@IDCampoEncuesta", campo.IDCampoEncuesta);

                            conn2.Open();
                            using (SqlDataReader reader2 = cmd2.ExecuteReader())
                            {
                                while (reader2.Read())
                                {
                                    RespuestaEncuesta respuesta = new RespuestaEncuesta
                                    {
                                        IDRespuestaEncuesta = Convert.ToInt32(reader2["IDRespuestaEncuesta"]),
                                        IDCampoEncuesta = Convert.ToInt32(reader2["IDCampoEncuesta"]),
                                        Respuesta = reader2["Respuesta"].ToString(),
                                        FechaRespuesta = Convert.ToDateTime(reader2["FechaRespuesta"])
                                    };
                                    campo.Resuestas.Add(respuesta);
                                }
                            }
                        }
                        campos.Add(campo);
                    }
                }
            }
            encuesta.Campos = campos;

            return View(encuesta);
        }


        public IActionResult RespuestaEncuestas()
        {
            List<Encuesta> encuestas = new List<Encuesta>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand command = new SqlCommand("GetAllEncuestas", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Encuesta encuesta = new Encuesta
                            {
                                IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                                Nombre = reader["Nombre"].ToString(),
                                Descripcion = reader["Descripcion"].ToString(),
                                IdAux = Encriptar(Convert.ToInt32(reader["IDEncuesta"]))
                            };
                            encuestas.Add(encuesta);
                        }
                    }
                }
            }

            return View(encuestas);
        }

        #endregion

        #region Responder Encuesta

        [AllowAnonymous]
        public IActionResult ResponderEncuesta(string Id)
        {
            int IdEncuesta = 0;
            try
            {
                IdEncuesta = Desencriptar(Id);
            }
            catch (Exception)
            {
                return StatusCode(404, "Not found");
            }

            Encuesta encuesta = null;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IdEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        encuesta = new Encuesta
                        {
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"].ToString()
                        };
                    }
                }
            }

            if (encuesta == null)
            {
                return StatusCode(404, "Not found");
            }
            List<CampoEncuesta> campos = new List<CampoEncuesta>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_SelectCamposPorEncuesta", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IDEncuesta", IdEncuesta);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CampoEncuesta campo = new CampoEncuesta
                        {
                            IDCampoEncuesta = Convert.ToInt32(reader["IDCampoEncuesta"]),
                            IDEncuesta = Convert.ToInt32(reader["IDEncuesta"]),
                            Titulo = reader["Titulo"].ToString(),
                            NombreCampo = reader["NombreCampo"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            TipoInput = reader["TipoInput"].ToString(),
                            Requerido = Convert.ToBoolean(reader["Requerido"])
                        };
                        campos.Add(campo);
                    }
                }
            }
            encuesta.Campos = campos;
            return View(encuesta);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult LlenarEncuesta()
        {
            List<RespuestaEncuesta> respuestas = new List<RespuestaEncuesta>();

            foreach (var key in Request.Form.Keys)
            {
                string valor = Request.Form[key];
                if (int.TryParse(key, out int idCampoEncuesta))
                {
                    respuestas.Add(new RespuestaEncuesta
                    {
                        IDCampoEncuesta = idCampoEncuesta,
                        Respuesta = valor
                    });
                }
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    foreach (var respuesta in respuestas)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_InsertRespuestaEncuesta", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@IDCampoEncuesta", respuesta.IDCampoEncuesta);
                            cmd.Parameters.AddWithValue("@Respuesta", respuesta.Respuesta);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }

            return RedirectToAction("FinEncuesta", "Encuesta");
        }


        [AllowAnonymous]
        public IActionResult FinEncuesta()
        {
            return View();
        }
        #endregion
    }
}

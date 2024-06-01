using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Claims;
using ProyectoEncuesta.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;

namespace ProyectoEncuesta.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Usuario value)
        {
            if (!ModelState.IsValid)
            {
                return View(); // Mantener errores de validación
            }

            // Validar credenciales del usuario usando el procedimiento almacenado
            bool esValido = await ValidarUsuario(value.Nombre, EncriptarString(value.Contraseña));

            if (esValido)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, value.Nombre)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Resultado"] = "Error";
                TempData["Mensaje"] = "Usuario o contraseña incorrectos";
                return RedirectToAction("Index", "Login");
            }
        }

        private async Task<bool> ValidarUsuario(string nombre, string contraseña)
        {
            bool esValido = false;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("sp_ValidarUsuario", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Nombre", nombre);
                    command.Parameters.AddWithValue("@Contraseña", contraseña);

                    var result = await command.ExecuteScalarAsync();

                    if (result != null && (int)result == 1)
                    {
                        esValido = true;
                    }
                }
            }

            return esValido;
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login"); // Redirigir a la página principal
        }

        public static string EncriptarString(string texto)
        {

            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }
    }
}

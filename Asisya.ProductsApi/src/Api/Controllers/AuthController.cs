using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(JwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validación simple - en producción usarías ASP.NET Identity
                // Usuario demo: admin@asisya.com / password123
                if (request.Email == "admin@asisya.com" && request.Password == "password123")
                {
                    var token = _jwtService.GenerateToken(request.Email);
                    var expiresAt = _jwtService.GetTokenExpiry();

                    var response = new LoginResponse
                    {
                        Token = token,
                        ExpiresAt = expiresAt,
                        UserEmail = request.Email
                    };

                    _logger.LogInformation("Usuario {Email} autenticado exitosamente", request.Email);

                    return Ok(new ApiResponse<LoginResponse>
                    {
                        Success = true,
                        Message = "Login exitoso",
                        Data = response
                    });
                }

                return Unauthorized(new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Credenciales inválidas",
                    Errors = "Email o contraseña incorrectos"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para {Email}", request.Email);
                return StatusCode(500, new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("test")]
        public ActionResult<ApiResponse<string>> Test()
        {
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "API funcionando correctamente",
                Data = $"Servidor funcionando - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
            });
        }
    }
}

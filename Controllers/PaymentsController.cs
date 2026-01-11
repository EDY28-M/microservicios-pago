using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGatewayService.DTOs;
using PaymentGatewayService.Services;
using System.Security.Claims;

namespace PaymentGatewayService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService, 
            IConfiguration configuration,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _configuration = configuration;
            _logger = logger;
        }

        private int GetUsuarioId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return int.Parse(userIdClaim);
        }

        /// <summary>
        /// Crea un Payment Intent para procesar el pago de cursos
        /// </summary>
        [HttpPost("create-intent")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePaymentIntent([FromBody] CreatePaymentIntentDto dto)
        {
            try
            {
                // Obtener el ID del estudiante desde el backend principal
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                if (idEstudiante == null)
                {
                    return Unauthorized(new { mensaje = "No se pudo obtener el ID del estudiante" });
                }

                var result = await _paymentService.CreatePaymentIntentAsync(idEstudiante.Value, dto);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear Payment Intent");
                return StatusCode(500, new { mensaje = "Error al crear Payment Intent", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el estado de un pago por PaymentIntentId
        /// </summary>
        [HttpGet("status/{paymentIntentId}")]
        public async Task<ActionResult<PaymentStatusDto>> GetPaymentStatus(string paymentIntentId)
        {
            try
            {
                var payment = await _paymentService.GetPaymentStatusAsync(paymentIntentId);

                if (payment == null)
                    return NotFound(new { mensaje = "Pago no encontrado" });

                // Verificar que el pago pertenece al usuario autenticado
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                var paymentEntity = await _paymentService.GetPaymentByIntentIdAsync(paymentIntentId);
                
                if (paymentEntity == null || (idEstudiante.HasValue && paymentEntity.IdEstudiante != idEstudiante.Value))
                {
                    return Forbid("No tienes permiso para ver este pago");
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado del pago");
                return StatusCode(500, new { mensaje = "Error al obtener estado del pago", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el historial de pagos del estudiante autenticado
        /// </summary>
        [HttpGet("historial")]
        public async Task<ActionResult<List<PaymentStatusDto>>> GetHistorial()
        {
            try
            {
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                if (idEstudiante == null)
                {
                    return Unauthorized(new { mensaje = "No se pudo obtener el ID del estudiante" });
                }

                var payments = await _paymentService.GetPaymentsByEstudianteAsync(idEstudiante.Value);

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de pagos");
                return StatusCode(500, new { mensaje = "Error al obtener historial de pagos", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crea un Payment Intent para pagar la matrícula (1 PEN)
        /// </summary>
        [HttpPost("pagar-matricula")]
        public async Task<ActionResult<PaymentResponseDto>> PagarMatricula([FromBody] PagarMatriculaDto dto)
        {
            try
            {
                // Obtener el ID del estudiante desde el backend principal
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                if (idEstudiante == null)
                {
                    return Unauthorized(new { mensaje = "No se pudo obtener el ID del estudiante" });
                }

                var result = await _paymentService.CreateMatriculaPaymentIntentAsync(idEstudiante.Value, dto.IdPeriodo);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear Payment Intent para matrícula");
                return StatusCode(500, new { mensaje = "Error al crear Payment Intent para matrícula", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Confirma un pago exitoso y lo marca como procesado
        /// Este endpoint se llama desde el frontend después de un pago exitoso en Stripe
        /// </summary>
        [HttpPost("confirm-payment/{paymentIntentId}")]
        public async Task<ActionResult> ConfirmPayment(string paymentIntentId)
        {
            try
            {
                // Obtener el ID del estudiante desde el backend principal
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                if (idEstudiante == null)
                {
                    return Unauthorized(new { mensaje = "No se pudo obtener el ID del estudiante" });
                }

                // Verificar que el pago existe y pertenece al estudiante
                var payment = await _paymentService.GetPaymentByIntentIdAsync(paymentIntentId);
                if (payment == null)
                {
                    return NotFound(new { mensaje = "Pago no encontrado" });
                }

                if (payment.IdEstudiante != idEstudiante.Value)
                {
                    return Forbid("No tienes permiso para confirmar este pago");
                }

                // Procesar el pago
                var success = await _paymentService.ProcessPaymentSuccessAsync(paymentIntentId);
                
                if (success)
                {
                    return Ok(new { mensaje = "Pago confirmado exitosamente", procesado = true });
                }
                else
                {
                    return BadRequest(new { mensaje = "Error al procesar el pago", procesado = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar pago {PaymentIntentId}", paymentIntentId);
                return StatusCode(500, new { mensaje = "Error al confirmar pago", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Verifica si el estudiante ha pagado la matrícula para el período (endpoint directo)
        /// Este endpoint recibe el idEstudiante directamente del backend principal
        /// </summary>
        [HttpGet("verificar-matricula-pagada/{idEstudiante}/{idPeriodo}")]
        [AllowAnonymous] // Permitir llamadas desde el backend principal sin JWT
        public async Task<ActionResult<bool>> VerificarMatriculaPagadaDirect(int idEstudiante, int idPeriodo)
        {
            try
            {
                _logger.LogInformation("Verificando matrícula pagada: idEstudiante={IdEstudiante}, idPeriodo={IdPeriodo}", 
                    idEstudiante, idPeriodo);

                var pagado = await _paymentService.HasPaidMatriculaAsync(idEstudiante, idPeriodo);

                _logger.LogInformation("Resultado verificación: pagado={Pagado}", pagado);

                return Ok(new { pagado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar pago de matrícula");
                return StatusCode(500, new { mensaje = "Error al verificar pago de matrícula", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Verifica si el estudiante ha pagado la matrícula para el período (legacy)
        /// </summary>
        [HttpGet("verificar-matricula-pagada/{idPeriodo}")]
        public async Task<ActionResult<bool>> VerificarMatriculaPagada(int idPeriodo)
        {
            try
            {
                // Obtener el ID del estudiante desde el backend principal
                var idEstudiante = await GetEstudianteIdFromBackendAsync();
                if (idEstudiante == null)
                {
                    return Ok(new { pagado = false });
                }

                var pagado = await _paymentService.HasPaidMatriculaAsync(idEstudiante.Value, idPeriodo);

                return Ok(new { pagado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar pago de matrícula");
                return StatusCode(500, new { mensaje = "Error al verificar pago de matrícula", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el ID del estudiante desde el backend principal usando el usuarioId del JWT
        /// </summary>
        private async Task<int?> GetEstudianteIdFromBackendAsync()
        {
            try
            {
                var backendUrl = _configuration["BackendPrincipal:BaseUrl"] ?? "http://localhost:5251";
                
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(backendUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                // Pasar el token JWT al backend
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await httpClient.GetAsync("/api/estudiantes/perfil");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var perfil = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                    if (perfil.TryGetProperty("id", out var id))
                    {
                        return id.GetInt32();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener ID del estudiante desde backend principal");
                return null;
            }
        }
    }
}

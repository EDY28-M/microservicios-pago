using System.Text;
using System.Text.Json;
using PaymentGatewayService.DTOs;
using PaymentGatewayService.Services;

namespace PaymentGatewayService.Services
{
    public class BackendIntegrationService : IBackendIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackendIntegrationService> _logger;

        public BackendIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<BackendIntegrationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = _configuration["BackendPrincipal:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("BackendPrincipal:BaseUrl no está configurada");
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Si hay API key configurada, usarla
            var apiKey = _configuration["BackendPrincipal:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        }

        public async Task<bool> MatricularEstudianteAsync(MatriculaPagoDto dto)
        {
            try
            {
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Llamando al backend principal para matricular estudiante {IdEstudiante} con pago {PaymentIntentId}",
                    dto.IdEstudiante, dto.StripePaymentIntentId);

                var response = await _httpClient.PostAsync("/api/estudiantes/matricular-pago", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Matrícula exitosa para estudiante {IdEstudiante}", dto.IdEstudiante);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al matricular estudiante {IdEstudiante}: {StatusCode} - {Error}",
                        dto.IdEstudiante, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al matricular estudiante {IdEstudiante}", dto.IdEstudiante);
                return false;
            }
        }
    }
}

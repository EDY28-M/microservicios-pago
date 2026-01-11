using PaymentGatewayService.DTOs;

namespace PaymentGatewayService.Services
{
    public interface IBackendIntegrationService
    {
        Task<bool> MatricularEstudianteAsync(MatriculaPagoDto dto);
    }
}

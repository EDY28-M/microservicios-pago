using PaymentGatewayService.DTOs;
using PaymentGatewayService.Models;

namespace PaymentGatewayService.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentIntentAsync(int idEstudiante, CreatePaymentIntentDto dto);
        Task<PaymentResponseDto> CreateMatriculaPaymentIntentAsync(int idEstudiante, int idPeriodo);
        Task<PaymentStatusDto?> GetPaymentStatusAsync(string paymentIntentId);
        Task<List<PaymentStatusDto>> GetPaymentsByEstudianteAsync(int idEstudiante);
        Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId);
        Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId);
        Task<bool> HasPaidMatriculaAsync(int idEstudiante, int idPeriodo);
    }
}

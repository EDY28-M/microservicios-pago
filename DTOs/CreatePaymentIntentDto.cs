using System.ComponentModel.DataAnnotations;

namespace PaymentGatewayService.DTOs
{
    public class CreatePaymentIntentDto
    {
        [Required]
        public int IdPeriodo { get; set; }

        [Required]
        [MinLength(1)]
        public List<CursoPagoDto> Cursos { get; set; } = new();

        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class CursoPagoDto
    {
        [Required]
        public int IdCurso { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        public int Cantidad { get; set; } = 1;
    }
}

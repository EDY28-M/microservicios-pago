using System.ComponentModel.DataAnnotations;

namespace PaymentGatewayService.DTOs
{
    public class PagarMatriculaDto
    {
        [Required]
        public int IdPeriodo { get; set; }
    }
}

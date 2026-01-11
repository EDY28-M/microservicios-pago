namespace PaymentGatewayService.DTOs
{
    public class MatriculaPagoDto
    {
        public int IdEstudiante { get; set; }
        public int IdPeriodo { get; set; }
        public List<int> IdsCursos { get; set; } = new();
        public string StripePaymentIntentId { get; set; } = string.Empty;
    }
}

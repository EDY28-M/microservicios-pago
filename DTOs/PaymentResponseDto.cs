namespace PaymentGatewayService.DTOs
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public List<PaymentItemDto> Items { get; set; } = new();
    }

    public class PaymentItemDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PaymentStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? FechaPagoExitoso { get; set; }
        public bool Procesado { get; set; }
        public string? ErrorMessage { get; set; }
        public List<PaymentItemDto> Items { get; set; } = new();
    }
}

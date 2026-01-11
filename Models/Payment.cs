using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentGatewayService.Models
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("idEstudiante")]
        public int IdEstudiante { get; set; }

        [Required]
        [Column("idPeriodo")]
        public int IdPeriodo { get; set; }

        [Required]
        [Column("stripe_payment_intent_id")]
        [MaxLength(255)]
        public string StripePaymentIntentId { get; set; } = string.Empty;

        [Column("stripe_customer_id")]
        [MaxLength(255)]
        public string? StripeCustomerId { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column("currency")]
        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        [Required]
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // pending, succeeded, failed, canceled

        [Column("payment_method")]
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [Column("metadata_json")]
        public string? MetadataJson { get; set; } // JSON con detalles de cursos

        [Required]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTime? FechaActualizacion { get; set; }

        [Column("fecha_pago_exitoso")]
        public DateTime? FechaPagoExitoso { get; set; }

        [Column("error_message")]
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        [Column("procesado")]
        public bool Procesado { get; set; } = false; // Flag para evitar procesamiento duplicado

        // Navigation properties
        public virtual ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
    }
}

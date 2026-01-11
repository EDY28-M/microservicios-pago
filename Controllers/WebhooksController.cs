using Microsoft.AspNetCore.Mvc;
using PaymentGatewayService.Services;
using Stripe;
using System.Text;

namespace PaymentGatewayService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IPaymentService paymentService,
            IStripeService stripeService,
            IConfiguration configuration,
            ILogger<WebhooksController> logger)
        {
            _paymentService = paymentService;
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para recibir webhooks de Stripe
        /// </summary>
        [HttpPost("stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("WebhookSecret no está configurada");
                return BadRequest(new { error = "WebhookSecret no configurada" });
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    webhookSecret
                );

                _logger.LogInformation("Webhook recibido: {EventType}, {EventId}", 
                    stripeEvent.Type, stripeEvent.Id);

                // Procesar según el tipo de evento
                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;

                    case "payment_intent.canceled":
                        await HandlePaymentIntentCanceled(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation("Evento no manejado: {EventType}", stripeEvent.Type);
                        break;
                }

                return Ok(new { received = true });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error de Stripe al procesar webhook: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar webhook: {Message}", ex.Message);
                return StatusCode(500, new { error = "Error al procesar webhook" });
            }
        }

        private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("PaymentIntent es null en evento succeeded");
                return;
            }

            _logger.LogInformation("Procesando pago exitoso: {PaymentIntentId}", paymentIntent.Id);

            // Actualizar estado en BD
            var payment = await _paymentService.GetPaymentByIntentIdAsync(paymentIntent.Id);
            if (payment != null)
            {
                payment.Status = paymentIntent.Status;
                payment.FechaActualizacion = DateTime.UtcNow;

                // Procesar matrícula
                await _paymentService.ProcessPaymentSuccessAsync(paymentIntent.Id);
            }
        }

        private async Task HandlePaymentIntentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            _logger.LogWarning("Pago fallido: {PaymentIntentId}", paymentIntent.Id);

            var payment = await _paymentService.GetPaymentByIntentIdAsync(paymentIntent.Id);
            if (payment != null)
            {
                payment.Status = "failed";
                payment.FechaActualizacion = DateTime.UtcNow;
                payment.ErrorMessage = paymentIntent.LastPaymentError?.Message ?? "Pago fallido";
            }
        }

        private async Task HandlePaymentIntentCanceled(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            _logger.LogInformation("Pago cancelado: {PaymentIntentId}", paymentIntent.Id);

            var payment = await _paymentService.GetPaymentByIntentIdAsync(paymentIntent.Id);
            if (payment != null)
            {
                payment.Status = "canceled";
                payment.FechaActualizacion = DateTime.UtcNow;
            }
        }
    }
}

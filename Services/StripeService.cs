using Stripe;
using PaymentGatewayService.Services;

namespace PaymentGatewayService.Services
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey no está configurada en appsettings.json");
            }

            StripeConfiguration.ApiKey = secretKey;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            Dictionary<string, string>? metadata,
            string? customerId = null)
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convertir a centavos
                    Currency = currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = metadata ?? new Dictionary<string, string>()
                };

                if (!string.IsNullOrEmpty(customerId))
                {
                    options.Customer = customerId;
                }

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation("PaymentIntent creado: {PaymentIntentId}, Amount: {Amount}", 
                    paymentIntent.Id, amount);

                return paymentIntent;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error al crear PaymentIntent: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                return paymentIntent;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error al obtener PaymentIntent {PaymentIntentId}: {Message}", 
                    paymentIntentId, ex.Message);
                throw;
            }
        }

        public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string webhookSecret)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    webhookSecret
                );

                return Task.FromResult(stripeEvent != null);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Error al verificar firma de webhook: {Message}", ex.Message);
                return Task.FromResult(false);
            }
        }
    }
}

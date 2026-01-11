using Stripe;

namespace PaymentGatewayService.Services
{
    public interface IStripeService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, Dictionary<string, string>? metadata, string? customerId = null);
        Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
        Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string webhookSecret);
    }
}

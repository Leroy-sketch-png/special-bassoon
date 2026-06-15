namespace MoePortal.Core.Interfaces;

public record CreatePaymentSessionRequest(
    Guid InvoiceId,
    decimal Amount,
    string Currency,         // Always "SGD"
    string PayerEmail,
    string RedirectUrl,
    string WebhookUrl
);

public record PaymentSessionResult(
    string? SessionId,
    string? CheckoutUrl,
    bool Success,
    string? ErrorMessage
);

public interface IPaymentProviderService
{
    Task<PaymentSessionResult> CreatePaymentSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct = default);
    Task<bool> VerifyWebhookSignatureAsync(string rawBodyWithoutHmac, string incomingSignature, CancellationToken ct = default);

    Task<string?> GetPaymentStatusAsync(string sessionId, CancellationToken ct = default);
}

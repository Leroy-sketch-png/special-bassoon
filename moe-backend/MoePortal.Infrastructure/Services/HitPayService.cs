using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MoePortal.Core.Interfaces;

namespace MoePortal.Infrastructure.Services;

public class HitPayService : IPaymentProviderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _webhookSalt;

    public HitPayService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["HitPay:ApiKey"] ?? throw new ArgumentNullException("HitPay API Key missing");
        _webhookSalt = config["HitPay:WebhookSalt"] ?? throw new ArgumentNullException("HitPay Webhook Salt missing");
    }

    public async Task<PaymentSessionResult> CreatePaymentSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            amount = request.Amount,
            currency = request.Currency,
            email = request.PayerEmail,
            reference_number = request.InvoiceId.ToString(),
            redirect_url = request.RedirectUrl,
            webhook = request.WebhookUrl
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/payment-requests")
        {
            Content = content
        };
        requestMessage.Headers.Add("X-BUSINESS-API-KEY", _apiKey);

        var response = await _httpClient.SendAsync(requestMessage, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new PaymentSessionResult(null, null, false, $"HitPay API Error: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(responseBody);
        
        var id = doc.RootElement.GetProperty("id").GetString();
        var url = doc.RootElement.GetProperty("url").GetString();

        return new PaymentSessionResult(id, url, true, null);
    }

    public Task<bool> VerifyWebhookSignatureAsync(string rawBodyWithoutHmac, string incomingSignature, CancellationToken ct = default)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSalt));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBodyWithoutHmac));
        var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return Task.FromResult(computedSignature.Equals(incomingSignature, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string?> GetPaymentStatusAsync(string sessionId, CancellationToken ct = default)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/v1/payment-requests/{sessionId}");
        requestMessage.Headers.Add("X-BUSINESS-API-KEY", _apiKey);
        var response = await _httpClient.SendAsync(requestMessage, ct);
        if (!response.IsSuccessStatusCode) return null;
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("status").GetString();
    }
}

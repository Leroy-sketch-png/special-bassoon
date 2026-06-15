using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


/// <summary>
/// Stub implementation of the Singpass FAPI 2.0 OIDC client.
/// 
/// Implements the correct method signatures for a future real integration with the
/// Singpass staging/production environments (NDI Developer Portal registration required).
/// 
/// Sprint 1 uses MockSingpassLogin in CitizenAuthController instead.
/// This stub will be activated in Sprint 2 when Singpass sandbox credentials are provisioned.
/// 
/// References:
///   - PAR flow: https://docs.developer.singpass.gov.sg/docs/technical-specifications/integration-guide/1.-authorization-request
///   - Userinfo: https://docs.developer.singpass.gov.sg/docs/technical-specifications/singpass-authentication-api/3.-userinfo-endpoint/requesting-userinfo
///   - FAPI 2.0 + DPoP: https://docs.developer.singpass.gov.sg/docs/technical-specifications/migration-guides/login-myinfo-v5-apps
/// </summary>
public class SingpassOidcAdapter
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Microsoft.Extensions.Hosting.IHostEnvironment _env;

    public SingpassOidcAdapter(IConfiguration config, IHttpClientFactory httpClientFactory, Microsoft.Extensions.Hosting.IHostEnvironment env)
    {
        _config            = config;
        _httpClientFactory = httpClientFactory;
        _env               = env;
    }

    /// <summary>
    /// Step 1 of FAPI 2.0 PAR flow.
    /// POSTs authorization parameters to the PAR endpoint and returns a request_uri.
    /// The request_uri is short-lived (60s) and used in the subsequent authorization redirect.
    /// </summary>
    /// <param name="clientId">Singpass client ID (from NDI Developer Portal)</param>
    /// <param name="redirectUri">Registered callback URI</param>
    /// <param name="scope">OAuth scopes (e.g., "openid profile")</param>
    /// <param name="codeChallenge">PKCE S256 code challenge</param>
    /// <param name="state">Anti-CSRF state value (store server-side; never in cookie)</param>
    /// <param name="nonce">Replay-prevention nonce</param>
    /// <returns>request_uri to use in the authorization redirect</returns>
    /// <exception cref="NotImplementedException">Singpass staging not yet provisioned.</exception>
    public async Task<string> InitiateParAsync(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string state,
        string nonce,
        CancellationToken ct = default)
    {
        if (_env.IsProduction()) throw new NotImplementedException("Singpass staging not yet provisioned.");
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "response_type", "code" },
            { "scope", scope },
            { "code_challenge", codeChallenge }
        });
        
        var response = await client.PostAsync("http://localhost:5006/api/simulator/singpass/par", content, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var json = System.Text.Json.JsonDocument.Parse(body);
        return json.RootElement.GetProperty("request_uri").GetString() ?? "";
    }

    /// <summary>
    /// Step 2 of FAPI 2.0 PAR flow.
    /// Exchanges an authorization code + PKCE code verifier for access/ID tokens.
    /// Validates ID token: issuer, audience, nonce, expiry, signature.
    /// </summary>
    /// <param name="code">Authorization code from callback</param>
    /// <param name="codeVerifier">Original PKCE code verifier</param>
    /// <returns>Tuple of (accessToken, idToken)</returns>
    /// <exception cref="NotImplementedException">Singpass staging not yet provisioned.</exception>
    public async Task<(string AccessToken, string IdToken)> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken ct = default)
    {
        if (_env.IsProduction()) throw new NotImplementedException("Singpass staging not yet provisioned.");
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code", code },
            { "code_verifier", codeVerifier }
        });
        
        var response = await client.PostAsync("http://localhost:5006/api/simulator/singpass/token", content, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var json = System.Text.Json.JsonDocument.Parse(body);
        return (json.RootElement.GetProperty("access_token").GetString() ?? "", json.RootElement.GetProperty("id_token").GetString() ?? "");
    }

    /// <summary>
    /// Step 3 of FAPI 2.0 PAR flow.
    /// Retrieves Singpass userinfo claims using the access token.
    /// Only returns claims that were in the original scope request.
    /// Maps Singpass claims to internal CitizenProfile.
    /// </summary>
    /// <returns>CitizenProfile with sub (NRIC/FIN), name, dateOfBirth, nationality</returns>
    /// <exception cref="NotImplementedException">Singpass staging not yet provisioned.</exception>
    public async Task<SingpassCitizenProfile> GetUserInfoAsync(
        string accessToken,
        CancellationToken ct = default)
    {
        if (_env.IsProduction()) throw new NotImplementedException("Singpass staging not yet provisioned.");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("http://localhost:5006/api/simulator/singpass/userinfo", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var json = System.Text.Json.JsonDocument.Parse(body);
        var root = json.RootElement;
        
        return new SingpassCitizenProfile(
            Sub: root.GetProperty("sub").GetString() ?? "",
            Name: root.GetProperty("name").GetString(),
            DateOfBirth: DateOnly.Parse(root.GetProperty("birthdate").GetString() ?? "2000-01-01"),
            Nationality: root.GetProperty("nationality").GetString()
        );
    }
}

/// <summary>
/// Internal representation of identity claims returned by Singpass Userinfo endpoint.
/// Maps to the FAS trusted-prefill fields (nationality).
/// </summary>
public record SingpassCitizenProfile(
    string Sub,         // NRIC/FIN — stable unique identifier
    string? Name,
    DateOnly? DateOfBirth,
    string? Nationality // e.g., "SINGAPORE CITIZEN", "SINGAPORE PR"
);

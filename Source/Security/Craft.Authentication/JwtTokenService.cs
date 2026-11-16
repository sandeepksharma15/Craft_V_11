using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Craft.Authentication;

public sealed class JwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _lifetime;
    private readonly TimeSpan _skew;
    private readonly SymmetricSecurityKey[] _keys;
    private readonly SigningCredentials _signing;
    private readonly TimeProvider _clock;
    private readonly ITokenRevocationStore _revocations;
    internal int MaximumTokenSize { get; }

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock, ITokenRevocationStore revocations)
    {
        ArgumentNullException.ThrowIfNull(options);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _revocations = revocations ?? throw new ArgumentNullException(nameof(revocations));
        var settings = options.Value;
        if (!JwtOptions.IsValid(settings)) throw new OptionsValidationException(nameof(JwtOptions), typeof(JwtOptions), ["Invalid JWT configuration."]);
        _issuer = settings.Issuer;
        _audience = settings.Audience;
        _lifetime = settings.Lifetime;
        _skew = settings.ClockSkew;
        MaximumTokenSize = settings.MaximumTokenSize;
        _keys = settings.SigningKeys.Select(p => new SymmetricSecurityKey(Convert.FromBase64String(p.Value)) { KeyId = p.Key }).ToArray();
        _signing = new SigningCredentials(_keys.Single(k => k.KeyId == settings.ActiveKeyId), SecurityAlgorithms.HmacSha256);
    }

    public string IssueAccessToken(string subject, IEnumerable<Claim>? claims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        var supplied = claims?.ToArray() ?? [];
        string[] reserved = ["sub", "jti", "iss", "aud", "exp", "nbf", "iat", ClaimTypes.NameIdentifier];
        if (supplied.Any(c => c is null || reserved.Contains(c.Type, StringComparer.Ordinal)))
            throw new ArgumentException("Claims cannot override token identity or validation metadata.", nameof(claims));
        var now = _clock.GetUtcNow().UtcDateTime;
        var payload = new List<Claim>(supplied) { new("sub", subject), new("jti", Guid.NewGuid().ToString("N")) };
        var token = new JwtSecurityToken(_issuer, _audience, payload, now, now.Add(_lifetime), _signing);
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (encoded.Length > MaximumTokenSize) throw new InvalidOperationException("Issued token exceeds MaximumTokenSize.");
        return encoded;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string? token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validated = ValidateSignature(token);
        return validated is { } value && await AcceptPrincipalAsync(value.Principal, cancellationToken).ConfigureAwait(false)
            ? value.Principal : null;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validated = ValidateSignature(token) ?? throw new SecurityTokenException("Invalid token.");
        var id = SingleClaim(validated.Principal, "jti");
        if (id is null || SingleClaim(validated.Principal, "sub") is null) throw new SecurityTokenException("Missing token identity.");
        await _revocations.RevokeAsync(RevocationKey(id), new DateTimeOffset(validated.Expires, TimeSpan.Zero).Add(_skew), cancellationToken).ConfigureAwait(false);
    }

    internal async Task<bool> AcceptPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var id = SingleClaim(principal, "jti");
        return id is not null && SingleClaim(principal, "sub") is not null &&
            !await _revocations.IsRevokedAsync(RevocationKey(id), cancellationToken).ConfigureAwait(false);
    }

    private static string? SingleClaim(ClaimsPrincipal principal, string type)
    {
        var values = principal.FindAll(type).ToArray();
        return values.Length == 1 && !string.IsNullOrWhiteSpace(values[0].Value) ? values[0].Value : null;
    }

    private string RevocationKey(string id) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { _issuer, id }))));

    private (ClaimsPrincipal Principal, DateTime Expires)? ValidateSignature(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenSize) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false, MaximumTokenSizeInBytes = MaximumTokenSize };
            var principal = handler.ValidateToken(token, CreateValidationParameters(), out var parsed);
            return (principal, parsed.ValidTo);
        }
        catch (SecurityTokenException) { return null; }
        catch (ArgumentException) { return null; }
    }

    internal TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true, ValidIssuer = _issuer,
        ValidateAudience = true, ValidAudience = _audience, IgnoreTrailingSlashWhenValidatingAudience = false,
        RequireSignedTokens = true, ValidateIssuerSigningKey = true, IssuerSigningKeys = _keys,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        RequireExpirationTime = true, ValidateLifetime = true, ClockSkew = _skew,
        NameClaimType = "name", RoleClaimType = "role",
        LifetimeValidator = (notBefore, expires, _, _) => expires.HasValue &&
            (!notBefore.HasValue || notBefore.Value <= expires.Value && notBefore.Value <= _clock.GetUtcNow().UtcDateTime.Add(_skew)) &&
            expires.Value > _clock.GetUtcNow().UtcDateTime.Subtract(_skew)
    };
}

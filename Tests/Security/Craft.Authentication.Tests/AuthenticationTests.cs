using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Craft.Core.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Craft.Authentication.Tests;

public class Clock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

public class AuthenticationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static JwtOptions Settings() => new() { Issuer = "issuer", Audience = "api", ActiveKeyId = "key-1", SigningKeys = new() { ["key-1"] = Convert.ToBase64String(new byte[32]) } };
    private static JwtTokenService Create(JwtOptions settings, Clock clock, ITokenRevocationStore? store = null) => new(Options.Create(settings), clock, store ?? new InMemoryTokenRevocationStore(clock));
    private static string Raw(JwtOptions settings, DateTime? from, DateTime? until, string? issuer = null, string? audience = null, string algorithm = SecurityAlgorithms.HmacSha256, bool signed = true, Claim[]? claims = null)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKeys[settings.ActiveKeyId])) { KeyId = settings.ActiveKeyId };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer ?? settings.Issuer, audience ?? settings.Audience,
            claims ?? [new("sub", "42"), new("jti", Guid.NewGuid().ToString("N"))], from, until, signed ? new SigningCredentials(key, algorithm) : null));
    }

    [Fact]
    public async Task IssueValidateRevoke_UsesOnePolicyAndRetainsRevocationThroughSkew()
    {
        var clock = new Clock(); var options = Settings(); var store = new InMemoryTokenRevocationStore(clock); var service = Create(options, clock, store);
        var token = service.IssueAccessToken("42", [new("role", "admin"), new("name", "Sandeep")]);
        var principal = await service.ValidateAsync(token, Token);
        Assert.NotNull(principal); Assert.Equal("42", principal.FindFirst("sub")!.Value); Assert.True(principal.IsInRole("admin")); Assert.Equal("Sandeep", principal.Identity!.Name);
        Assert.NotEqual(token, service.IssueAccessToken("42"));
        await service.RevokeAsync(token, Token);
        clock.Now += options.Lifetime + TimeSpan.FromSeconds(10);
        Assert.Null(await service.ValidateAsync(token, Token));
        clock.Now += options.ClockSkew;
        Assert.Null(await service.ValidateAsync(token, Token));
        await Assert.ThrowsAsync<SecurityTokenException>(() => service.RevokeAsync("bad", Token));
    }

    [Fact]
    public async Task Validation_RejectsInvalidSignaturesClaimsAndLifetimes()
    {
        var clock = new Clock(); var options = Settings(); var service = Create(options, clock); var now = clock.Now.UtcDateTime;
        string?[] invalid = [null, "", "bad", "a.b.c", new string('x', options.MaximumTokenSize + 1),
            Raw(options, now, now.AddMinutes(1), issuer: "wrong"), Raw(options, now, now.AddMinutes(1), audience: "wrong"), Raw(options, now, now.AddMinutes(1), audience: "api/"),
            Raw(options, now, now.AddMinutes(1), signed: false), Raw(options, null, null),
            Raw(options, now.AddMinutes(2), now.AddMinutes(3)), Raw(options, now.AddHours(-1), now.AddMinutes(-1)),
            Raw(options, now, now.AddMinutes(1), claims: [new("sub", "42")]),
            Raw(options, now, now.AddMinutes(1), claims: [new("jti", "1")]),
            Raw(options, now, now.AddMinutes(1), claims: [new("jti", " "), new("sub", "42")]),
            Raw(options, now, now.AddMinutes(1), claims: [new("jti", "1"), new("sub", "42"), new("sub", "43")])];
        foreach (var token in invalid) Assert.Null(await service.ValidateAsync(token, Token));
        var wrongKey = Settings(); wrongKey.SigningKeys["key-1"] = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray());
        Assert.Null(await service.ValidateAsync(Raw(wrongKey, now, now.AddMinutes(1)), Token));
        var missingId = Raw(options, now, now.AddMinutes(1), claims: [new("sub", "42")]);
        await Assert.ThrowsAsync<SecurityTokenException>(() => service.RevokeAsync(missingId, Token));
        await Assert.ThrowsAsync<SecurityTokenException>(() => service.RevokeAsync(Raw(options, now, now.AddMinutes(1), claims: [new("jti", "1")]), Token));
        Assert.NotNull(await service.ValidateAsync(Raw(options, null, now.AddMinutes(1)), Token));
    }

    [Fact]
    public async Task Validation_EnforcesAlgorithmAndExactExpiryAndFailsClosed()
    {
        var clock = new Clock();
        var options = Settings();
        options.SigningKeys["key-1"] = Convert.ToBase64String(new byte[64]);
        var service = Create(options, clock);
        var now = clock.Now.UtcDateTime;
        Assert.Null(await service.ValidateAsync(Raw(options, now, now.AddMinutes(1), algorithm: SecurityAlgorithms.HmacSha512), Token));
        var token = service.IssueAccessToken("42");
        clock.Now += options.Lifetime + options.ClockSkew - TimeSpan.FromSeconds(1);
        Assert.NotNull(await service.ValidateAsync(token, Token));
        clock.Now += TimeSpan.FromSeconds(1);
        Assert.Null(await service.ValidateAsync(token, Token));
        var unavailable = Create(options, clock, new UnavailableStore());
        token = unavailable.IssueAccessToken("42");
        await Assert.ThrowsAsync<InvalidOperationException>(() => unavailable.ValidateAsync(token, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => unavailable.RevokeAsync(token, Token));
    }

    private sealed class UnavailableStore : ITokenRevocationStore
    {
        public Task<bool> IsRevokedAsync(string key, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unavailable");
        public Task RevokeAsync(string key, DateTimeOffset retainUntil, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unavailable");
    }

    [Fact]
    public async Task KeysAndOptions_AreSnapshotsAndPermitRotation()
    {
        var clock = new Clock(); var options = Settings(); var original = Create(options, clock); var token = original.IssueAccessToken("42");
        options.SigningKeys["key-2"] = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray()); options.ActiveKeyId = "key-2";
        var rotated = Create(options, clock);
        Assert.NotNull(await rotated.ValidateAsync(token, Token));
        Assert.Null(await original.ValidateAsync(rotated.IssueAccessToken("42"), Token));
        options.Issuer = "changed"; options.SigningKeys.Clear();
        Assert.NotNull(await original.ValidateAsync(token, Token));
    }

    [Fact]
    public async Task ArgumentsAndCancellation_AreEnforced()
    {
        var clock = new Clock(); var options = Settings(); var store = new InMemoryTokenRevocationStore(clock); var service = Create(options, clock);
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(null!, clock, store));
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(Options.Create(options), null!, store));
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(Options.Create(options), clock, null!));
        Assert.Throws<ArgumentException>(() => service.IssueAccessToken(" "));
        Assert.Throws<ArgumentException>(() => service.IssueAccessToken("42", [new("sub", "override")]));
        Assert.Throws<ArgumentException>(() => service.IssueAccessToken("42", [null!]));
        Assert.Throws<InvalidOperationException>(() => service.IssueAccessToken("42", [new("big", new string('x', options.MaximumTokenSize))]));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ValidateAsync("invalid", new CancellationToken(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RevokeAsync("invalid", new CancellationToken(true)));
    }

    [Fact]
    public async Task AspNetAuthentication_ValidatesAndRevokesTheActualBearerToken()
    {
        var services = new ServiceCollection(); var clock = new Clock();
        services.AddLogging(); services.AddSingleton<TimeProvider>(clock);
        services.AddInMemoryTokenRevocation(); services.AddInMemoryTokenRevocation();
        services.AddCraftAuthentication(o => { var s = Settings(); o.Issuer = s.Issuer; o.Audience = s.Audience; o.ActiveKeyId = s.ActiveKeyId; o.SigningKeys = s.SigningKeys; });
        using var provider = services.BuildServiceProvider();
        var tokens = provider.GetRequiredService<JwtTokenService>(); var token = tokens.IssueAccessToken("42");
        async Task<AuthenticateResult> Authenticate(string value)
        {
            using var scope = provider.CreateScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Headers.Authorization = "bearer " + value;
            return await context.AuthenticateAsync(ServiceCollectionExtensions.Scheme);
        }
        Assert.True((await Authenticate(token)).Succeeded);
        await tokens.RevokeAsync(token, Token);
        Assert.False((await Authenticate(token)).Succeeded);
        Assert.False((await Authenticate("bad")).Succeeded);
        Assert.Single(provider.GetServices<ITokenRevocationStore>());
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCraftAuthentication(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCraftAuthentication(null!));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddInMemoryTokenRevocation(null!));
    }

    [Fact]
    public async Task InMemoryStore_ExtendsDeadlinesAndExpiresEntries()
    {
        var clock = new Clock(); var store = new InMemoryTokenRevocationStore(clock);
        Assert.Throws<ArgumentNullException>(() => new InMemoryTokenRevocationStore(null!));
        Assert.False(await store.IsRevokedAsync("one", Token));
        await store.RevokeAsync("one", clock.Now.AddMinutes(3), Token);
        await store.RevokeAsync("one", clock.Now.AddMinutes(1), Token);
        clock.Now += TimeSpan.FromMinutes(2); Assert.True(await store.IsRevokedAsync("one", Token));
        await store.RevokeAsync("one", clock.Now.AddMinutes(3), Token);
        clock.Now += TimeSpan.FromMinutes(4); Assert.False(await store.IsRevokedAsync("one", Token));
        await store.RevokeAsync("past", clock.Now.AddMinutes(-1), Token);
        Assert.False(await store.IsRevokedAsync("past", Token));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.IsRevokedAsync(" ", Token));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.RevokeAsync(" ", clock.Now, Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.IsRevokedAsync("one", new CancellationToken(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.RevokeAsync("one", clock.Now, new CancellationToken(true)));
    }
}

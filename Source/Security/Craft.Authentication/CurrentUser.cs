using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using Craft.Core.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Craft.Authentication;

public class CurrentUser<TKey>(IHttpContextAccessor accessor) : ICurrentUser<TKey> where TKey : notnull, IParsable<TKey>
{
    private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    private IEnumerable<ClaimsIdentity> Identities => _accessor.HttpContext?.User.Identities.Where(i => i.IsAuthenticated) ?? [];
    public bool IsAuthenticated => Identities.Any();
    public string? Name => Identities.FirstOrDefault()?.Name;

    public bool TryGetId([MaybeNullWhen(false)] out TKey id)
    {
        id = default;
        var ids = Identities.SelectMany(i => i.Claims).Where(c => c.Type is "sub" or ClaimTypes.NameIdentifier)
            .Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray();
        return ids.Length == 1 && !string.IsNullOrWhiteSpace(ids[0]) && TKey.TryParse(ids[0], CultureInfo.InvariantCulture, out id);
    }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : CurrentUser<KeyType>(accessor), ICurrentUser;

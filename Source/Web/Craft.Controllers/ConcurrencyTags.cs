using System.Globalization;
using System.Text;
using Craft.Domain.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Craft.Controllers;

public static class ConcurrencyTags
{
    public static string? Get(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is not IHasConcurrency concurrency)
        {
            if (entity is not IHasVersion versioned) return null;
            if (versioned.Version < 1) throw new InvalidOperationException("Versioned endpoints require configured, persisted positive versions.");
            return "\"v:" + versioned.Version.ToString(CultureInfo.InvariantCulture) + "\"";
        }
        if (string.IsNullOrWhiteSpace(concurrency.ConcurrencyStamp))
            throw new InvalidOperationException("Concurrency-enabled endpoints require configured, persisted concurrency stamps.");
        return "\"" + WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(concurrency.ConcurrencyStamp)) + "\"";
    }

    internal static int? Validate(object entity, string? supplied)
    {
        var current = Get(entity);
        if (current is null) return null;
        if (string.IsNullOrWhiteSpace(supplied)) return 428;
        if (!EntityTagHeaderValue.TryParse(supplied, out var tag) || tag.Tag == "*") return 400;
        return !tag.IsWeak && string.Equals(tag.ToString(), current, StringComparison.Ordinal) ? null : 412;
    }
}

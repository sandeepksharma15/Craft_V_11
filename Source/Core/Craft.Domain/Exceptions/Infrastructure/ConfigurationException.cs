using System.Net;

namespace Craft.Domain.Exceptions.Infrastructure;

/// <summary>A configuration error occurred. HTTP 500.</summary>
public class ConfigurationException : CraftException
{
    public ConfigurationException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "A configuration error occurred", HttpStatusCode.InternalServerError, innerException, errors) { }

    public ConfigurationException(string configurationKey, string reason)
        : this($"Configuration error for key \"{configurationKey}\": {reason}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);
    }
}
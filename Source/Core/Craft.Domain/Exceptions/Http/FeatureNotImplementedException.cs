using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>This feature is not implemented. HTTP 501.</summary>
public class FeatureNotImplementedException : CraftException
{
    public FeatureNotImplementedException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "This feature is not implemented", HttpStatusCode.NotImplemented, innerException, errors) { }

    public FeatureNotImplementedException(string featureName, string details)
        : this($"Feature \"{featureName}\" is not implemented: {details}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
    }
}
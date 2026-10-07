using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>The media type is not supported. HTTP 415.</summary>
public class UnsupportedMediaTypeException : CraftException
{
    public UnsupportedMediaTypeException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The media type is not supported", HttpStatusCode.UnsupportedMediaType, innerException, errors) { }

    public UnsupportedMediaTypeException(string mediaType, string[] supportedTypes)
        : this($"Media type \"{mediaType}\" is not supported. Supported types: {string.Join(", ", supportedTypes)}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
    }
}
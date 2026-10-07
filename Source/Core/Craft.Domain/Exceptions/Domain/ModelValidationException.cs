using System.Collections.ObjectModel;
using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Validation failure with immutable errors keyed by property name. HTTP 400.</summary>
public class ModelValidationException : CraftException
{
    public ModelValidationException(string? message = null, Exception? innerException = null,
        IEnumerable<string>? errors = null)
        : base(message ?? "One or more validation failures have occurred.", HttpStatusCode.BadRequest, innerException, errors)
    {
        ValidationErrors = ReadOnlyDictionary<string, IReadOnlyList<string>>.Empty;
    }

    public ModelValidationException(IDictionary<string, string[]> validationErrors, string? message = null,
        Exception? innerException = null)
        : this(message ?? "One or more validation failures have occurred.", Snapshot(validationErrors), innerException) { }

    private ModelValidationException(string message, IReadOnlyDictionary<string, IReadOnlyList<string>> validationErrors,
        Exception? innerException)
        : base(message, HttpStatusCode.BadRequest, innerException, validationErrors.Values.SelectMany(errors => errors))
    {
        ValidationErrors = validationErrors;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors { get; }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Snapshot(IDictionary<string, string[]> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        Dictionary<string, IReadOnlyList<string>> snapshot = new(StringComparer.Ordinal);

        foreach ((string property, string[] errors) in validationErrors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            snapshot.Add(property, Array.AsReadOnly(errors.ToArray()));
        }

        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(snapshot);
    }
}
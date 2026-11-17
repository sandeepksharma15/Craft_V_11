using System.ComponentModel.DataAnnotations;

namespace Craft.Controllers;

public sealed record EntityReference<TKey>([property: Required] TKey Id, string? IfMatch = null) where TKey : notnull;
public sealed record EntityUpdate<TKey, TUpdate>([property: Required] TKey Id,
    [property: Required] TUpdate Model, string? IfMatch = null) where TKey : notnull where TUpdate : class;
public enum EntityOperation { Read, Create, Update, Delete, Restore }

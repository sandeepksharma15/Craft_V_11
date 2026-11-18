using System.ComponentModel.DataAnnotations;

namespace Craft.Controllers;

public sealed record EntityReference<TKey>([Required] TKey Id, string? IfMatch = null) where TKey : notnull;
public sealed record EntityUpdate<TKey, TUpdate>([Required] TKey Id,
    [Required] TUpdate Model, string? IfMatch = null) where TKey : notnull where TUpdate : class;
public enum EntityOperation { Read, Create, Update, Delete, Restore }

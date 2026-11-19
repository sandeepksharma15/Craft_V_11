namespace Craft.HttpServices;

public sealed record EntityReference<TKey>(TKey Id, string? IfMatch = null) where TKey : notnull;

public sealed record EntityUpdate<TKey, TUpdate>(TKey Id, TUpdate Model, string? IfMatch = null)
    where TKey : notnull
    where TUpdate : class;

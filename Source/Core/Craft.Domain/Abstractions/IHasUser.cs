namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that are associated with a specific user.</summary>
public interface IHasUser<TKey>
{
    /// <summary>
    /// The name of the database column for the UserId property.
    /// </summary>
    public const string ColumnName = "UserId";

    TKey UserId { get; set; }

    TKey GetUserId() => UserId;

    /// <summary>
    /// Determines whether the user identifier is set to a non-default value.
    /// </summary>
    bool IsUserIdSet() => !EqualityComparer<TKey>.Default.Equals(UserId, default);

    void SetUserId(TKey userId) => UserId = userId;
}

/// <summary>Defines a contract for entities that are associated with a user with the default long identifier.</summary>
public interface IHasUser : IHasUser<long>;

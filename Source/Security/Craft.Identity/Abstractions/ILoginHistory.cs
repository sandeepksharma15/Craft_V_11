namespace Craft.Identity.Abstractions;

public interface ILoginHistory<TKey> : IHasId<TKey>, IHasUser<TKey>, ISoftDelete, IEntity<TKey>, IModel<TKey>
{
    #region Public Properties

    public string? LastIpAddress { get; set; }

    public DateTime? LastLoginOn { get; set; }

    public string? Provider { get; set; }

    #endregion Public Properties
}

public interface ILoginHistory : ILoginHistory<KeyType>, IHasId, IHasUser, IEntity, IModel;

namespace Craft.Identity.Abstractions;

public interface ICraftRole<TKey> : IEntity<TKey>, ISoftDelete, IHasConcurrency, IHasActive, IModel<TKey>
{
    #region Public Properties

    public string? Description { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }

    #endregion Public Properties
}

public interface ICraftRole : ICraftRole<KeyType>, IEntity, IModel;

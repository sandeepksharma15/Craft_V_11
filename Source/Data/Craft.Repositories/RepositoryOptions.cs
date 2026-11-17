namespace Craft.Repositories;

public sealed class RepositoryOptions
{
    public int MaximumPageSize { get; set; } = 200;
    public int MaximumListSize { get; set; } = 1000;
    public int MaximumBatchSize { get; set; } = 1000;

    internal static bool IsValid(RepositoryOptions options) => options.MaximumPageSize > 0 &&
        options.MaximumListSize is > 0 and < int.MaxValue && options.MaximumBatchSize is > 0 and < int.MaxValue;
}

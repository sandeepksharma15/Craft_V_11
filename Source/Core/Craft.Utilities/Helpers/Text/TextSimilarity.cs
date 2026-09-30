namespace Craft.Utilities.Helpers;

/// <summary>
/// Provides case-sensitive edit distance over UTF-16 code units.
/// </summary>
public static class TextSimilarity
{
    #region Public Methods

    /// <summary>
    /// Computes Levenshtein distance (insertions, deletions and substitutions). Null is treated as
    /// empty. Uses O(min(source.Length, target.Length)) working memory.
    /// </summary>
    public static int LevenshteinDistance(string? source, string? target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        if (source.Length > target.Length)
            (source, target) = (target, source);

        int[] row = new int[source.Length + 1];

        for (int i = 0; i < row.Length; i++)
            row[i] = i;

        for (int j = 1; j <= target.Length; j++)
        {
            int diagonal = row[0];
            row[0] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                int above = row[i];
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                row[i] = Math.Min(Math.Min(row[i - 1], above) + 1, diagonal + cost);
                diagonal = above;
            }
        }

        return row[^1];
    }

    #endregion Public Methods
}

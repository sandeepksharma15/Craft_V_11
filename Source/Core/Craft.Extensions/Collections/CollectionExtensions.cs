#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class CollectionExtensions
{
    extension<T>(ICollection<T>? source)
    {
        /// <summary>
        /// Determines whether the specified collection is null or empty.
        /// </summary>
        public bool IsNullOrEmpty()
            => source is null || source.Count == 0;

        /// <summary>
        /// Adds the specified item to the collection if it does not already exist in the collection.
        /// </summary>
        public bool AddIfNotContains(T item)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));

            if (source.Contains(item))
                return false;

            source.Add(item);
            return true;
        }

        /// <summary>
        /// Adds the specified items to the collection if they are not already present.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the items that were successfully added to the collection. If no items
        /// were added, the returned collection will be empty.</returns>
        public IEnumerable<T> AddIfNotContains(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(items, nameof(items));

            List<T> addedItems = [];

            foreach (T item in items)
            {
                if (source.Contains(item))
                    continue;

                source.Add(item);
                addedItems.Add(item);
            }

            return addedItems;
        }

        /// <summary>
        /// Adds an item to the collection if no existing item satisfies the specified predicate.
        /// </summary>
        public bool AddIfNotContains(Func<T, bool> predicate, Func<T> itemFactory)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
            ArgumentNullException.ThrowIfNull(itemFactory, nameof(itemFactory));

            if (source.Any(predicate))
                return false;

            source.Add(itemFactory());

            return true;
        }

        /// <summary>
        /// Removes all elements in the specified collection from the source collection.
        /// </summary>
        public void RemoveAll(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(items, nameof(items));

            foreach (T? item in items)
                _ = source.Remove(item);
        }
    }
}

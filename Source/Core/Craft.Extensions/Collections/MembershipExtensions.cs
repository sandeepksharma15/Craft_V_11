#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class MembershipExtensions
{
    extension<T>(T item)
    {
        /// <summary>
        /// Check if an item is in a collection.
        /// </summary>
        public bool IsIn(IEnumerable<T> collection)
            => collection.Contains(item);

        /// <summary>
        /// Check if an item is in a list.
        /// </summary>
        public bool IsIn(params T[] list)
            => list.Contains(item);
    }
}

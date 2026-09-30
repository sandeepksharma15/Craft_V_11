using System.Runtime.InteropServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T>? source)
    {

        /// <summary>
        /// Creates a new list containing every source item in randomized order.
        /// </summary>
        /// <remarks>
        /// Enumerates the source once and leaves it unchanged. Duplicate and null items are preserved.
        /// Uses non-cryptographic randomness and does not guarantee a different order on each call.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public List<T> GenerateRandomizedList()
        {
            ArgumentNullException.ThrowIfNull(source);

            List<T> randomizedList = [.. source];
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(randomizedList));
            return randomizedList;
        }

        /// <summary>
        /// Converts a collection of objects into a dictionary suitable for use in a select list, where the keys represent
        /// the values and the values represent the display text.
        /// </summary>
        public Dictionary<string, string> GetListDataForSelect(string valueField, string displayField)
        {
            Dictionary<string, string> listItems = [];

            foreach (T item in source ?? [])
            {
                if (valueField != null && displayField != null)
                {
                    string strValue = item?.GetType()!.GetProperty(valueField)?.GetValue(item)?.ToString() ?? string.Empty;
                    string strDisplay = item?.GetType()?.GetProperty(displayField)?.GetValue(item)?.ToString() ?? string.Empty;

                    listItems.Add(strValue, strDisplay);
                }
                else
                {
                    if (item is not null)
                        listItems.Add(item.ToString()!, item.ToString() ?? string.Empty);
                    else
                        listItems.Add(string.Empty, string.Empty);
                }
            }

            return listItems;
        }
    }
}

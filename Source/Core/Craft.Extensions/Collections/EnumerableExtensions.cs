#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T>? source)
    {
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

    extension<T>(T item)
    {
        /// <summary>
        /// Check if an item is in a collection.
        /// </summary>
        public bool IsIn(IEnumerable<T> collection)
            => collection.Contains(item);
    }
}

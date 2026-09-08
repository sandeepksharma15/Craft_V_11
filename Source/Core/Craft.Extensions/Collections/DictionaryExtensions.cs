using System.Globalization;

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System.Collections.Generic;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        /// <summary>
        /// Gets the value associated with the specified key, or adds and returns a new value if the key does not exist.
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(valueFactory);

            if (dictionary.TryGetValue(key, out TValue? value))
                return value;

            value = valueFactory(key);
            dictionary.Add(key, value);

            return value;
        }

        /// <summary>
        /// Gets the value associated with the specified key, or adds and returns a default value if the key does not exist.
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue defaultValue)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (dictionary.TryGetValue(key, out TValue? value))
                return value;

            dictionary.Add(key, defaultValue);

            return defaultValue;
        }

        /// <summary>
        /// Adds or updates the value associated with the specified key.
        /// </summary>
        public void AddOrUpdate(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            dictionary[key] = value;
        }

        /// <summary>
        /// Merges the specified source dictionary into the current dictionary. If a key already exists,
        /// it will be overwritten based on the overwriteExisting parameter.
        /// </summary>
        public IDictionary<TKey, TValue> Merge(IDictionary<TKey, TValue> source, bool overwriteExisting = true)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(dictionary);

            foreach (KeyValuePair<TKey, TValue> kvp in source)
                if (overwriteExisting || !dictionary.ContainsKey(kvp.Key))
                    dictionary[kvp.Key] = kvp.Value;

            return dictionary;
        }

        /// <summary>
        /// Converts a dictionary to a query string format (e.g., "key1=value1&amp;key2=value2").
        /// </summary>
        public string ToQueryString()
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            return dictionary.Count == 0
                ? string.Empty
                : string.Join("&", dictionary
                    .Select(static kvp => new KeyValuePair<string, string>(
                        Uri.EscapeDataString(FormatQueryStringValue(kvp.Key)),
                        Uri.EscapeDataString(FormatQueryStringValue(kvp.Value))))
                    .OrderBy(static kvp => kvp.Key, StringComparer.Ordinal)
                    .Select(static kvp => $"{kvp.Key}={kvp.Value}"));
        }

        /// <summary>
        /// Safely removes a key from the dictionary and returns whether the key was present.
        /// </summary>
        public bool TryRemove(TKey key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            return dictionary.Remove(key);
        }

        /// <summary>
        /// Safely removes a key from the dictionary and returns the removed value if present.
        /// </summary>
        public bool TryRemove(TKey key, out TValue? value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (dictionary.TryGetValue(key, out value))
            {
                _ = dictionary.Remove(key);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Creates a shallow copy of the dictionary.
        /// </summary>
        public Dictionary<TKey, TValue> Clone()
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            return dictionary is Dictionary<TKey, TValue> concreteDictionary
                ? new(concreteDictionary, concreteDictionary.Comparer)
                : new(dictionary);
        }
    }

    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        /// <summary>
        /// Inverts the dictionary, swapping keys and values. If duplicate values exist, the last occurrence is kept.
        /// </summary>
        public Dictionary<TValue, TKey> Invert()
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            Dictionary<TValue, TKey> inverted = [with(dictionary.Count)];

            foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
                inverted[kvp.Value] = kvp.Key;

            return inverted;
        }
    }

    private static string FormatQueryStringValue<T>(T value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}

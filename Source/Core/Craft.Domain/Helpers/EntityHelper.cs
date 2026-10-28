using System.Diagnostics.CodeAnalysis;
using Craft.Domain.Abstractions;

namespace Craft.Domain.Helpers;

public static class EntityHelper
{
    extension([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        public bool IsEntity()
        {
            ArgumentNullException.ThrowIfNull(type);
            return type.HasImplementedInterface(typeof(IEntity<>));
        }

        public bool IsEntity<TKey>()
        {
            ArgumentNullException.ThrowIfNull(type);
            return typeof(IEntity<TKey>).IsAssignableFrom(type);
        }

        public bool IsMultiTenant()
        {
            ArgumentNullException.ThrowIfNull(type);
            return type.HasImplementedInterface(typeof(IHasTenant<>));
        }

        public bool IsMultiTenant<TKey>()
        {
            ArgumentNullException.ThrowIfNull(type);
            return typeof(IHasTenant<TKey>).IsAssignableFrom(type);
        }

        /// <summary>Gets the unique entity key type, including a type parameter for open generic entities.</summary>
        public Type GetEntityKeyType()
            => type.TryGetEntityKeyType(out var keyType)
                ? keyType
                : throw new ArgumentException("The type must implement exactly one IEntity<TKey> contract.", nameof(type));

        /// <summary>Returns false for non-entities or multiple entity key contracts.</summary>
        public bool TryGetEntityKeyType([NotNullWhen(true)] out Type? keyType)
        {
            ArgumentNullException.ThrowIfNull(type);
            keyType = null;

            if (type.IsGenericTypeOf(typeof(IEntity<>)))
            {
                keyType = type.GetGenericArguments()[0];
                return true;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericTypeOf(typeof(IEntity<>)))
                    continue;

                if (keyType is not null)
                {
                    keyType = null;
                    return false;
                }

                keyType = contract.GetGenericArguments()[0];
            }

            return keyType is not null;
        }
    }
}
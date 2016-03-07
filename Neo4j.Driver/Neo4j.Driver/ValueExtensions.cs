using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver
{
    /// <summary>
    /// A collection of extensions to process values streamed back via Bolt
    /// </summary>
    public static class ValueExtensions
    {
        /// <summary>
        /// A helper method to explicitly cast the value streamed back via Bolt to a local type.
        /// </summary>
        /// <typeparam name="T">
        /// Well support for one of the following types (or nullable if applies):
        /// <see cref="short"/>,
        /// <see cref="int"/>,
        /// <see cref="long"/>,
        /// <see cref="float"/>,
        /// <see cref="double"/>,
        /// <see cref="sbyte"/>,
        /// <see cref="ushort"/>,
        /// <see cref="uint"/>,
        /// <see cref="ulong"/>,
        /// <see cref="byte"/>,
        /// <see cref="char"/>,
        /// <see cref="bool"/>,
        /// <see cref="string"/>, 
        /// <see cref="INode"/>,
        /// <see cref="IRelationship"/>,
        /// <see cref="IPath"/>.
        /// Undefined support for other types that are not listed above.
        /// No support for user-defined types, e.g. Person, Movie.
        /// </typeparam>
        /// <param name="value">The value that streamed back via Bolt protocol, e.g.<see cref="IEntity.Properties"/></param>
        /// <returns>The value of type <see cref="T"/></returns>
        public static T As<T>(this object value)
        {
            if (value == null)
            {
                if (default(T) == null)
                {
                    return default(T);
                }
                throw new NotSupportedException($"Unsupported cast from `null` to `{typeof(T)}`");
            }
            if (value is T)
            {
                return (T) value;
            }

            // if the user want to force cast
            var sourceType = value.GetType();
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (targetType == typeof (string))
            {
                return value.ToString().AsItIs<T>();
            }
            if (targetType == typeof (short))
            {
                return Convert.ToInt16(value).AsItIs<T>();
            }
            if (targetType == typeof (int))
            {
                return Convert.ToInt32(value).AsItIs<T>();
            }
            if (targetType == typeof (long))
            {
                return Convert.ToInt64(value).AsItIs<T>();
            }
            if (targetType == typeof (float))
            {
                return Convert.ToSingle(value).AsItIs<T>();
            }
            if (targetType == typeof (double))
            {
                return Convert.ToDouble(value).AsItIs<T>();
            }
            if (targetType == typeof (sbyte))
            {
                return Convert.ToSByte(value).AsItIs<T>();
            }
            if (targetType == typeof (ulong))
            {
                return Convert.ToUInt64(value).AsItIs<T>();
            }
            if (targetType == typeof (uint))
            {
                return Convert.ToUInt32(value).AsItIs<T>();
            }
            if (targetType == typeof (ushort))
            {
                return Convert.ToUInt16(value).AsItIs<T>();
            }
            if (targetType == typeof (byte))
            {
                return Convert.ToByte(value).AsItIs<T>();
            }
            if (targetType == typeof (char))
            {
                return Convert.ToChar(value).AsItIs<T>();
            }
            if (targetType == typeof (bool))
            {
                return Convert.ToBoolean(value).AsItIs<T>();
            }
            throw new NotSupportedException($"Unsupported cast from `{sourceType}` to `{typeof(T)}`");
        }
        /// <summary>
        /// A helper method to explicitly cast the value streamed back via Bolt to a list of items,
        /// where how the items are created are defined by the mapping function provided.
        /// </summary>
        /// <typeparam name="TV">The type of the items in the List</typeparam>
        /// <param name="value">The value that streamed back via Bolt protocol, e.g.<see cref="IEntity.Properties"/></param>
        /// <param name="mapFunc">the function that how to map each list item.</param>
        /// <returns></returns>
        public static IList<TV> AsList<TV>(this object value, Func<object, TV> mapFunc)
        {
            if (value is IList<object> || value is IReadOnlyList<object>)
            {
                var list = (from object item in (IList)value select mapFunc(item)).ToList();
                return list;
            }
            throw new NotSupportedException($"Unsupported cast from `{value.GetType()}` to `{typeof(IList<TV>)}`");
        }

        private static T AsItIs<T>(this object value)
        {
            if (value is T)
            {
                return (T)value;
            }
            throw new NotSupportedException($"Unsupported cast from `{value.GetType()}` to `{typeof(T)}`");
        }
    }
}

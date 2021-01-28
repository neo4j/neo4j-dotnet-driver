// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Neo4j.Driver
{
    /// <summary>
    /// A collection of extensions to process values streamed back via Bolt.
    /// </summary>
    public static class ValueExtensions
    {
        private static readonly TypeInfo EnumerableTypeInfo = typeof(IEnumerable).GetTypeInfo();
        private static readonly TypeInfo DictionaryTypeInfo = typeof(IDictionary).GetTypeInfo();

        /// <summary>
        /// A helper method to explicitly cast the value streamed back via Bolt to a local type,
        /// with default fallback value.
        /// </summary>
        /// <param name="value">The value that streamed back via Bolt protocol, e.g.<see cref="IEntity.Properties"/>.</param>
        /// <param name="defaultValue">Returns this value if the the value is null</param>
        /// <typeparam name="T">
        /// Supports for the following types (or nullable version of the following types if applies):
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
        /// <see cref="List{T}"/>,
        /// <see cref="INode"/>,
        /// <see cref="IRelationship"/>,
        /// <see cref="IPath"/>.
        /// Undefined support for other types that are not listed above.
        /// No support for user-defined types, e.g. Person, Movie.
        /// </typeparam>
        /// <returns>The value of specified return type.</returns>
        /// <remarks>Throws <see cref="InvalidCastException"/> if the specified cast is not possible.</remarks>
        public static T As<T>(this object value, T defaultValue)
        {
            return value == null ? defaultValue : value.As<T>();
        }

        /// <summary>
        /// A helper method to explicitly cast the value streamed back via Bolt to a local type.
        /// </summary>
        /// <typeparam name="T">
        /// Supports for the following types (or nullable version of the following types if applies):
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
        /// <see cref="List{T}"/>,
        /// <see cref="INode"/>,
        /// <see cref="IRelationship"/>,
        /// <see cref="IPath"/>.
        /// Undefined support for other types that are not listed above.
        /// No support for user-defined types, e.g. Person, Movie.
        /// </typeparam>
        /// <param name="value">The value that streamed back via Bolt protocol, e.g.<see cref="IEntity.Properties"/>.</param>
        /// <returns>The value of specified return type.</returns>
        /// <remarks>Throws <see cref="InvalidCastException"/> if the specified cast is not possible.</remarks>
        public static T As<T>(this object value)
        {
            if (value == null)
            {
                if (default(T) == null)
                {
                    return default(T);
                }
                throw new InvalidCastException($"Unable to cast `null` to `{typeof(T)}`.");
            }

            if (value is T returnValue)
            {
                return returnValue;
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
                return Convert.ToSingle(value, CultureInfo.InvariantCulture).AsItIs<T>();
            }
            if (targetType == typeof (double))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture).AsItIs<T>();
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
            if (value is IConvertible)
            {
                return Convert.ChangeType(value, targetType).AsItIs<T>();
            }

            // force to cast to a dict or list
            var typeInfo = targetType.GetTypeInfo();
            if (DictionaryTypeInfo.IsAssignableFrom(typeInfo) && typeInfo.IsGenericType && value is IDictionary dictionary)
            {
                return AsDictionary<T>(dictionary, typeInfo);
            }
            if (EnumerableTypeInfo.IsAssignableFrom(typeInfo) && typeInfo.IsGenericType && value is IEnumerable enumerable)
            {
                return AsList<T>(enumerable, typeInfo);
            }

            throw new InvalidCastException($"Unable to cast object of type `{sourceType}` to type `{typeof(T)}`.");
        }

        private static T AsDictionary<T>(this IDictionary dict, TypeInfo typeInfo)
        {
            var typeArguments = typeInfo.GenericTypeArguments;
            var dictionary =
                (IDictionary) Activator.CreateInstance(
                    typeof(Dictionary<,>).MakeGenericType(typeArguments));
            var asMethodForKey = GetInvocableAsMethod(typeArguments[0]);
            var asMethodForVal = GetInvocableAsMethod(typeArguments[1]);

            var enumerator = dict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                dictionary.Add(asMethodForKey.InvokeStatic(enumerator.Key), asMethodForVal.InvokeStatic(enumerator.Value));
            }

            return dictionary.AsItIs<T>();
        }

        private static T AsList<T>(this IEnumerable value, TypeInfo typeInfo)
        {
            var typeArguments = typeInfo.GenericTypeArguments;
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArguments));
            var asMethodForElement = GetInvocableAsMethod(typeArguments[0]);

            foreach (var o in value)
            {
                list.Add(asMethodForElement.InvokeStatic(o));
            }

            return list.AsItIs<T>();
        }

        #region Helper Methods

        private static object InvokeStatic(this MethodInfo method, params object[] parameters)
        {
            return method.Invoke(null, parameters);
        }

        private static MethodInfo GetInvocableAsMethod(params Type[] genericParameters)
        {
            return typeof(ValueExtensions).GetRuntimeMethod("As", genericParameters).MakeGenericMethod(genericParameters);
        }

        private static T AsItIs<T>(this object value)
        {
            if (value is T result)
            {
                return result;
            }
            throw new InvalidOperationException($"The expected value `{typeof(T)}` is different from the actual value `{value.GetType()}`");
        }
        
        #endregion Helper Methods

    }
}

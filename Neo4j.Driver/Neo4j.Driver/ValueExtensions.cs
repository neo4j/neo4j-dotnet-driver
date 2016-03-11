using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Exceptions;

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
        /// Well support for one of the following types (or nullable version of the following types if applies):
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
        /// <param name="value">The value that streamed back via Bolt protocol, e.g.<see cref="INode.Properties"/></param>
        /// <returns>The value of specified return type</returns>
        /// <remarks>Throws <see cref="InvalidCastException"/> if the specified cast is not possible</remarks>
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

            // force to cast to a dict or list
            var typeInfo = targetType.GetTypeInfo();
            if (typeInfo.ImplementedInterfaces.Contains(typeof(IDictionary)) && typeInfo.IsGenericType && value is IDictionary)
            {
                return value.AsDictionary<T>(typeInfo);
            }
            if (typeInfo.ImplementedInterfaces.Contains(typeof(IList)) && typeInfo.IsGenericType && value is IList)
            {
                return AsList<T>(value, typeInfo);
            }

            throw new InvalidCastException($"Unable to cast object of type `{sourceType}` to type `{typeof(T)}`.");
        }

        private static T AsDictionary<T>(this object value, TypeInfo typeInfo)
        {
            var dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments));
            foreach (var kvp in (IDictionary<string, object>)value)
            {
                dictionary.InvokeAddOnDictionary(kvp, typeInfo.GenericTypeArguments);
            }
            return dictionary.AsItIs<T>();
        }

        private static void InvokeAddOnDictionary(this object dict, KeyValuePair<string, object> toAdd, Type[] genericParameters)
        {
            if (!(dict is IDictionary))
                throw new InvalidOperationException("Unable to call 'Add' on something that's not a Dictionary.");

            var methodKey = GetInvokableAsMethod(genericParameters[0]);
            var methodVal = GetInvokableAsMethod(genericParameters[1]);

            dict.GetType().GetRuntimeMethod("Add", genericParameters).Invoke(dict, new[] { methodKey.InvokeStatic(toAdd.Key), methodVal.InvokeStatic(toAdd.Value) });
        }

        private static T AsList<T>(this object value, TypeInfo typeInfo)
        {
            var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments));

            foreach (var o in (IList)value)
            {
                list.InvokeAddOnList(o, typeInfo.GenericTypeArguments);
            }

            return list.AsItIs<T>();
        }

        private static void InvokeAddOnList(this object list, object toAdd, Type[] genericParameters)
        {
            if (!(list is IList))
            {
                throw new InvalidOperationException("Unable to call 'Add' on something that's not a list.");
            }

            var method = GetInvokableAsMethod(genericParameters);
            list.GetType().GetRuntimeMethod("Add", genericParameters).Invoke(list, new[] { method.InvokeStatic(toAdd) });
        }

        #region Helper Methods
        private static object InvokeStatic(this MethodInfo method, params object[] parameters)
        {
            return method.Invoke(null, parameters);
        }

        private static MethodInfo GetInvokableAsMethod(params Type[] genericParameters)
        {
            return typeof(ValueExtensions).GetRuntimeMethod("As", genericParameters).MakeGenericMethod(genericParameters);
        }

        private static T AsItIs<T>(this object value)
        {
            if (value is T)
            {
                return (T)value;
            }
            throw new InvalidOperationException($"The expected value `{typeof(T)}` is different from the actual value `{value.GetType()}`");
        }
        #endregion Helper Methods
    }
}

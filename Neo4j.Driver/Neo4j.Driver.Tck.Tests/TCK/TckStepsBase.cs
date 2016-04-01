using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    public class TckStepsBase
    {
        public const string Url = "bolt://localhost:7687";
        protected static Driver Driver;
        protected static INeo4jInstaller Installer;

        protected static object GetValue(string type, string value)
        {
            switch (type)
            {
                case "Null":
                    return null;
                case "Boolean":
                    return Convert.ToBoolean(value);
                case "Integer":
                    return Convert.ToInt64(value);
                case "Float":
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                case "String":
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type {type}");
            }
        }

        protected static void AssertEqual(object value, object other)
        {
            if (value == null || value is bool || value is long || value is double || value is string)
            {
                Assert.Equal(value, other);
            }
            else if (value is IList)
            {
                var valueList = (IList)value;
                var otherList = (IList)other;
                AssertEqual(valueList.Count, otherList.Count);
                for (var i = 0; i < valueList.Count; i++)
                {
                    AssertEqual(valueList[i], otherList[i]);
                }
            }
            else if (value is IDictionary)
            {
                var valueDic = (IDictionary<string, object>)value;
                var otherDic = (IDictionary<string, object>)other;
                AssertEqual(valueDic.Count, otherDic.Count);
                foreach (var key in valueDic.Keys)
                {
                    otherDic.ContainsKey(key).Should().BeTrue();
                    AssertEqual(valueDic[key], otherDic[key]);
                }
            }
        }

        protected static void DisposeDriver()
        {
            Driver?.Dispose();
        }

        protected static void CreateNewDriver()
        {
            Driver = GraphDatabase.Driver(Url);
        }
    }
}
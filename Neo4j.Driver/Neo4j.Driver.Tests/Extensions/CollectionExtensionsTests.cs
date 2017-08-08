using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Extensions
{
    public class CollectionExtensionsTests
    {

        public class ToDictionaryMethod
        {

            [Fact]
            public void ShouldReturnNullGivenNull()
            {
                var dict = CollectionExtensions.ToDictionary(null);

                dict.Should().BeNull();
            }

            [Theory]
            [InlineData((sbyte)0)]
            [InlineData((byte)0)]
            [InlineData((short)0)]
            [InlineData((ushort)0)]
            [InlineData((int)0)]
            [InlineData((uint)0)]
            [InlineData((long)0)]
            [InlineData((ulong)0)]
            [InlineData((char)0)]
            [InlineData((float)0)]
            [InlineData((double)0)]
            [InlineData(true)]
            public void ShouldHandleSimpleTypes(object value)
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = value
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue(value);
            }

            [Fact]
            public void ShouldHandleString()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = "value"
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue("value");
            }

            [Fact]
            public void ShouldHandleArray()
            {
                var array = new byte[2];

                var dict = CollectionExtensions.ToDictionary(new
                {
                    key = array
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("key");
                dict.Should().ContainValue(array);
            }

            [Fact]
            public void ShouldHandleAnonymousObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new { Name = "name", Surname = "surname" });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(2);
                dict.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Name", "name"),
                    new KeyValuePair<string, object>("Surname", "surname")
                });
            }

            [Fact]
            public void ShouldHandlePoco()
            {
                var dict = CollectionExtensions.ToDictionary(new MyPOCO() {Name = "name", Surname = "surname"});

                dict.Should().NotBeNull();
                dict.Should().HaveCount(2);
                dict.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Name", "name"),
                    new KeyValuePair<string, object>("Surname", "surname")
                });
            }

            [Fact]
            public void ShouldHandleDeeperObjects()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    DateOfBirth = new {Day = 31, Month = 12, Year = 2000}
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("DateOfBirth");

                var dateOfBirthObject = dict["DateOfBirth"];
                dateOfBirthObject.Should().NotBeNull();
                dateOfBirthObject.Should().BeAssignableTo<IDictionary<string, object>>();

                var dateOfBirth = (IDictionary<string, object>) dateOfBirthObject;
                dateOfBirth.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Day", 31),
                    new KeyValuePair<string, object>("Month", 12),
                    new KeyValuePair<string, object>("Year", 2000)
                });
            }

            [Fact]
            public void ShouldHandleDictionary()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    DateOfBirth = new Dictionary<string, object>()
                    {
                        {"Day", 31},
                        {"Month", 12},
                        {"Year", 2000}
                    }
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("DateOfBirth");

                var dateOfBirthObject = dict["DateOfBirth"];
                dateOfBirthObject.Should().NotBeNull();
                dateOfBirthObject.Should().BeAssignableTo<IDictionary<string, object>>();

                var dateOfBirth = (IDictionary<string, object>)dateOfBirthObject;
                dateOfBirth.Should().Contain(new[]
                {
                    new KeyValuePair<string, object>("Day", 31),
                    new KeyValuePair<string, object>("Month", 12),
                    new KeyValuePair<string, object>("Year", 2000)
                });
            }

            [Fact]
            public void ShouldHandleCollections()
            {
                var dict = CollectionExtensions.ToDictionary(new
                {
                    EmployeeIds = new List<int>() {1, 2, 3}
                });

                dict.Should().NotBeNull();
                dict.Should().HaveCount(1);
                dict.Should().ContainKey("EmployeeIds");

                var employeeIdsObject = dict["EmployeeIds"];
                employeeIdsObject.Should().NotBeNull();
                employeeIdsObject.Should().BeAssignableTo<IList<int>>();

                var employeeIds = (IList<int>)employeeIdsObject;
                employeeIds.Should().Contain(new[] {1, 2, 3});
            }

            private class MyPOCO
            {
                public string Name { get; set; }

                public string Surname { get; set; }
            }
        }

    }
}

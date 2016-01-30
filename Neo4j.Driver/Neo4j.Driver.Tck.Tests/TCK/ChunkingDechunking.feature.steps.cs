using System;
using System.Collections.Generic;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.IntegrationTests.TCK
{
    [Binding]
    public class DriverChunkingAndDeChunkingTestSteps : TckStepsBase
    {
        [Given(@"a String of size (.*)")]
        public void GivenAStringOfSize(int stringLength)
        {
            _expected = new string('a', stringLength);
        }
        
        [Given(@"a _list of size (.*) and type (.*)")]
        public void GivenAListOfSizeAndTypeNull(int size, string type)
        {

            string value;
            switch (type)
            {
                case "Null":
                    value = "null";
                    break;
                case "Boolean":
                    value = "true";
                    break;
                case "Integer":
                    value = "10";
                    break;
                case "Float":
                    value = "10.10";
                    break;
                case "String":
                    value = "lalalalalala";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type {type}");
            }
            var itemInList = GetValue(type, value);
            var list = new List<object>(size);
            for (int i = 0; i < size; i ++)
            {
                list.Add(itemInList);
            }
            _list = list;
        }
        
        [Given(@"a _map of size (.*) and type (.*)")]
        public void GivenAMapOfSizeAndTypeNull(int size, string type)
        {
            string value;
            switch (type)
            {
                case "Null":
                    value = "null";
                    break;
                case "Boolean":
                    value = "true";
                    break;
                case "Integer":
                    value = "10";
                    break;
                case "Float":
                    value = "10.10";
                    break;
                case "String":
                    value = "lalalalalala";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type {type}");
            }
            var itemInMap = GetValue(type, value);
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < size; i++)
            {
                dict.Add("Key" + i, itemInMap);
            }
            _map = dict;
        }
    }
}

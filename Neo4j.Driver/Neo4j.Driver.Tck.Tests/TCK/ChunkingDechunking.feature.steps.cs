﻿// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Collections.Generic;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
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
            for (var i = 0; i < size; i ++)
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
            for (var i = 0; i < size; i++)
            {
                dict.Add("Key" + i, itemInMap);
            }
            _map = dict;
        }
    }
}
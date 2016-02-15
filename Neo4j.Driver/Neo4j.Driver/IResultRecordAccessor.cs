//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    /// <summary>
    /// Access an underlying <see cref="IRecord"/>
    /// </summary>
    public interface IResultRecordAccessor
    {
        /// <summary>
        /// Retrieve the value of the property with the given index
        /// </summary>
        /// <param name="index">The position in the <see cref="IRecord"/></param>
        /// <returns>the value of property found with the index</returns>
        dynamic Get(int index);
        /// <summary>
        /// Retrieve the value of the property with the given key
        /// </summary>
        /// <param name="key">The key of the property</param>
        /// <returns>the value of property found with the provided key</returns>
        dynamic Get(string key);
        /// <summary>
        /// Test if the <see cref="IRecord"/> contains the given key
        /// </summary>
        /// <param name="key">A key of a property</param>
        /// <returns><c>true</c> if the key is found in the <see cref="IRecord"/></returns>
        bool ContainsKey(string key);
        /// <summary>
        /// Gets all the key in the <see cref="IRecord"/>
        /// </summary>
        IReadOnlyList<string> Keys { get; }
        /// <summary>
        /// Get the position of the property with the given key in the <see cref="IRecord"/>
        /// </summary>
        /// <param name="key">A key of a property</param>
        /// <returns>The position of the property in the <see cref="IRecord"/> or -1 if not found</returns>
        int Index(string key);
        /// <summary>
        /// Retrieve the number of values in this <see cref="IRecord"/>
        /// </summary>
        /// <returns>the number of values in this <see cref="IRecord"/></returns>
        int Size();
        /// <summary>
        /// Returns all the values in the <see cref="IRecord"/> in a <see cref="IReadOnlyDictionary{TKey,TValue}"/>
        /// </summary>
        /// <returns>all the values in the <see cref="IRecord"/></returns>
        IReadOnlyDictionary<string, dynamic> Values();
        /// <summary>
        /// Returns all the values in the <see cref="IRecord"/> in an ordered <see cref="IReadOnlyList{T}"/>
        /// </summary>
        /// <returns>all the values in the <see cref="IRecord"/> in an ordered <see cref="IReadOnlyList{T}"/></returns>
        IReadOnlyList<KeyValuePair<string, dynamic>> OrderedValues();
        bool HasRecord();
        /// <summary>
        /// Returns the underlying <see cref="IRecord"/> directly 
        /// </summary>
        /// <returns>the underlying <see cref="IRecord"/> directly</returns>
        IRecord Record();
    }
}
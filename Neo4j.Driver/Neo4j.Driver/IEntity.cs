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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    /// <summary>
    /// The base class for Node and Relationship
    /// </summary>
    public interface IEntity
    {
        IIdentity Identity { get; }
        IReadOnlyDictionary<string, object> Properties { get; }
    }

    public interface INode: IEntity
    {
        IReadOnlyList<string> Labels { get; }
        //bool HasLabel(string label);
    }

    public interface IRelationship:IEntity
    {
        string Type { get; }
        //bool HasType(string type);
        IIdentity Start { get; }
        IIdentity End { get; }
    }

    public interface IPath
    {
        INode Start { get; }
        INode End { get; }
        //int Length();
        //bool Contains(INode node);
        //bool Contains(IRelationship rel);
        IReadOnlyList<INode> Nodes { get; }
        IReadOnlyList<IRelationship> Relationships { get; }
    }

    public interface ISegment
    {
        INode Start { get; }
        INode End { get; }
        IRelationship Relationship { get; }
    }

    public interface IIdentity
    {
        // bool equal(object id)
        // int hashCode();
        long Id { get; }
    }
}

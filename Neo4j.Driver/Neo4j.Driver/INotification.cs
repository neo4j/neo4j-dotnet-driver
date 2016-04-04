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
namespace Neo4j.Driver
{
    /// <summary>
    /// Representation for notifications found when executing a statement.
    /// 
    /// A notification can be visualized in a client pinpointing problems or other information about the statement.
    /// </summary>
    public interface INotification
    {
        /// <summary>
        ///Gets a notification code for the discovered issue.
        /// </summary>
        string Code { get; }

        /// <summary>
        ///Gets a short summary of the notification.
        /// </summary>
        string Title { get; }

        /// <summary>
        ///Gets a longer description of the notification.
        /// </summary>
        string Description { get; }

        /// <summary>
        ///Gets the position in the statement where this notification points to.
        ///Not all notifications have a unique position to point to and in that case the position would be set to null.
        /// </summary>
        IInputPosition Position { get; }

        /// <summary>
        /// Gets The severity level of the notification.
        /// </summary>
        string Severity { get; }
    }

    /// <summary>
    /// An input position refers to a specific character in a statement.
    /// </summary>
    public interface IInputPosition
    {
        /// <summary>
        /// The character offset referred to by this position; offset numbers start at 0.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// The line number referred to by the position; line numbers start at 1.
        /// </summary>
        int Line { get; }

        /// <summary>
        /// The column number referred to by the position; column numbers start at 1.
        /// </summary>
        int Column { get; }
    }
}
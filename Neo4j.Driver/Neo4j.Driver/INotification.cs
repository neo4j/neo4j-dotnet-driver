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
    public interface INotification
    {
        ///
        ///Returns a notification code for the discovered issue.
        ///
        string Code { get; }

        ///
        ///Returns a short summary of the notification.
        ///
        string Title { get; }

        ///
        ///Returns a longer description of the notification.
        ///
        string Description { get; }

        ///
        ///The position in the statement where this notification points to.
        ///Not all notifications have a unique position to point to and in that case the position would be set to null.
        ///
        IInputPosition Position { get; }
    }

    public interface IInputPosition
    {
        ///
        /// The character offset referred to by this position; offset numbers start at 0.
        ///
        int Offset { get; }

        ///
        /// The line number referred to by the position; line numbers start at 1.
        ///
        int Line { get; }

        ///
        /// The column number referred to by the position; column numbers start at 1.
        ///
        int Column { get; }
    }
}
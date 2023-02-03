// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Neo4j.Driver.Reactive")]
[assembly: AssemblyDescription("The official .NET driver extension that provides Reactive API.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Neo4j.Driver")]
[assembly: AssemblyCopyright("Copyright ©  2002-2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]

#if STRONGNAMED
[assembly: InternalsVisibleTo("Neo4j.Driver.Tests, publicKey=00240000048000001401000006020000002400005253413100080000010001006594f23a327c08fea1433f7bffd1c4ecbcefdabbf2ac022ad2cc514f7c10102e9ff4263c6d9e4a9f06e9a9ffe1609e7362d7a8e7ff8559e967a5726e0da4c76f5f705e0eb1777fc77d3ddc0414dc33bcb3140e1847bfc43c29d8a83233218daa2eb3fb3aa81d685a6c4ea624bc1fe20274779843d1408084c8654727cd998d9090b0a4a6309eeca873ae49ab65a8d4e9199c1c8860f52611c726abcc116e78e2d31a1ed9d37320ca1d5877324eed6c2cb95ce13e74f2b8aec0873291a7ca63e712439485d214f67d41eab0e8c480b8e98cdda2e1bd082c5e81d845457d02251bfb39ad1e87e555834b2ebc82270948cf369af7a241e9f80169b52206030c99c1")]
[assembly: InternalsVisibleTo("Neo4j.Driver.Tests.Integration, publicKey=00240000048000001401000006020000002400005253413100080000010001006594f23a327c08fea1433f7bffd1c4ecbcefdabbf2ac022ad2cc514f7c10102e9ff4263c6d9e4a9f06e9a9ffe1609e7362d7a8e7ff8559e967a5726e0da4c76f5f705e0eb1777fc77d3ddc0414dc33bcb3140e1847bfc43c29d8a83233218daa2eb3fb3aa81d685a6c4ea624bc1fe20274779843d1408084c8654727cd998d9090b0a4a6309eeca873ae49ab65a8d4e9199c1c8860f52611c726abcc116e78e2d31a1ed9d37320ca1d5877324eed6c2cb95ce13e74f2b8aec0873291a7ca63e712439485d214f67d41eab0e8c480b8e98cdda2e1bd082c5e81d845457d02251bfb39ad1e87e555834b2ebc82270948cf369af7a241e9f80169b52206030c99c1")]
[assembly: InternalsVisibleTo("Neo4j.Driver.Tests.TestBackend, PublicKey=00240000048000001401000006020000002400005253413100080000010001006594f23a327c08fea1433f7bffd1c4ecbcefdabbf2ac022ad2cc514f7c10102e9ff4263c6d9e4a9f06e9a9ffe1609e7362d7a8e7ff8559e967a5726e0da4c76f5f705e0eb1777fc77d3ddc0414dc33bcb3140e1847bfc43c29d8a83233218daa2eb3fb3aa81d685a6c4ea624bc1fe20274779843d1408084c8654727cd998d9090b0a4a6309eeca873ae49ab65a8d4e9199c1c8860f52611c726abcc116e78e2d31a1ed9d37320ca1d5877324eed6c2cb95ce13e74f2b8aec0873291a7ca63e712439485d214f67d41eab0e8c480b8e98cdda2e1bd082c5e81d845457d02251bfb39ad1e87e555834b2ebc82270948cf369af7a241e9f80169b52206030c99c1")]
// Required for Moq to function in Unit Tests
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=00240000048000001401000006020000002400005253413100080000010001006594f23a327c08fea1433f7bffd1c4ecbcefdabbf2ac022ad2cc514f7c10102e9ff4263c6d9e4a9f06e9a9ffe1609e7362d7a8e7ff8559e967a5726e0da4c76f5f705e0eb1777fc77d3ddc0414dc33bcb3140e1847bfc43c29d8a83233218daa2eb3fb3aa81d685a6c4ea624bc1fe20274779843d1408084c8654727cd998d9090b0a4a6309eeca873ae49ab65a8d4e9199c1c8860f52611c726abcc116e78e2d31a1ed9d37320ca1d5877324eed6c2cb95ce13e74f2b8aec0873291a7ca63e712439485d214f67d41eab0e8c480b8e98cdda2e1bd082c5e81d845457d02251bfb39ad1e87e555834b2ebc82270948cf369af7a241e9f80169b52206030c99c1")]
#else
[assembly: InternalsVisibleTo("Neo4j.Driver.Tests")]
[assembly: InternalsVisibleTo("Neo4j.Driver.Tests.Integration")]
// Required for Moq to function in Unit Tests
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

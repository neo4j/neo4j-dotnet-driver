// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Neo4j.Driver.IntegrationTests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Neo4j.Driver.IntegrationTests")]
[assembly: AssemblyCopyright("Copyright ©  2002-2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("02f68df2-0047-4b04-93b6-521bd12b5d45")]

// The integration tests defined in this assembly require a database service running in the background.
// The tests might rely on certain status of the database, therefore the tests should be executed sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

#if STRONGNAMED
[assembly: AssemblySignatureKey(
    "002400000c800000140100000602000000240000525341310008000001000100d988a5d518f3e57d84d18057687648d5a4317ddd9cec3bdcb91d78565957c86e47804d3bb1fb9dd5318ccff5c25201ac130e84407f56d0f699f239bb4254a1ab40bb7baf2453c0f0316ed4f330e24a2983aced0f64ec7f281ef26a4df06039cf55b341c29e27361d83a3d4d5b1279a5b1ad4061ae7585623c08fc36a3f12bf4fa9447d4931a82d12ea913c99f614c17d86f3dfa1f5f10e36fb14f06a769c29e0bb18e3b3813275af3328cbf5bfc4ebd55589b5f25afde1dac8eccf7c1996a032ba6dc09d96a9377e88b5bc85aa2596fcdb45d3bb56ec225a832b1b8ae8f0e588a2bc04a98a208752cccb6772711bf34c8cf4a4b779a44ebb39b0c6a0a184cae7",
    "136d782a15af084ba55c3a4d38bbefa7afe42502043cea43b89736703d08208503c89121f693935aad8a36bb2e608e58b6f6ee7f858bc0558385f20fc691ecf2640c64c304eddf1a74a7a37c45a9ed2aa5ae7a6fad3a50961d167fd7004c2547a2a1690258fd110ed851107bfeed2084d05b6f29b2bbbaeece1e816dce850911ee2c4f5e8b1ecd9b130a6433dc750eeb34b80173718384ca2b9a00675de59661d165171c5907ffbd876525381025376b72008935dede0f7a6c7bc8258cb9aa99a0160d0eff372d4ff59cb24d3095d197bf85653835b44500786dc208e685a54f86ee86b415c978c65d392a3a29dfe67aa2e25801f04d827917b55590216e8d18")]
#endif
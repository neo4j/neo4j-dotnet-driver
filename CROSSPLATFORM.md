# Cross Platform Support

Although Neo4j.Driver package has been targeting .NET Standard 1.3 for a long time (thus being runnable on several platforms), with latest developments on Visual Studio, Rider and cross platform support on .NET platform, we are now developing this specific driver across different platforms (across Windows, Linux and MacOS).

This document describes what is required on your side to enable development on your platform of choice.

As of 1.5.0 release, the driver itself is targeting .NET Framework 4.5.2 and [.NET Standard 1.3](https://github.com/dotnet/standard/blob/master/docs/versions.md). The unit and integration tests are targeting both .NET Framework 4.5.2 and .NET Core App 1.0, where our TCK tests only support .NET Framework 4.5.2 because of its dependencies.

## Windows

Developing on Windows is straight forward with an installation of Visual Studio 2017 on your machine.

No IDE setup will probably very similar to the one described below under Linux section provided that you have all of the necessary .NET Framework 4.5.2 and .NET Core components installed.

## Linux

On Linux, we develop using the latest Rider release from Jetbrains. You should have latest [Mono](http://www.mono-project.com/download/#download-lin) version and at least [.NET Core](https://www.microsoft.com/net/download/core) 1.0 SDK and latest 1.0 runtime installed on your local machine as per instructed on their corresponding download sites.

#### IDE

Open the solution file (Neo4j.Driver.sln) with Rider and build the solution (at this stage Rider may ask you to update your settings such as msbuild and framework support). You should have a successful build after completing these steps.

**_Please note that Rider 2017.1.1 does not discover unit tests as expected and running tests through the IDE will not execute all known tests._**

#### No IDE

1. Go to ```Neo4j.Driver``` directory which is under the top level directory of 1.5.0 branch.
2. Invoke ```dotnet restore``` to get any dependencies to be resolved and cached.
3. Invoke ```dotnet build``` to build the driver along with tests. If this command generates an error saying that the reference assemblies for framework ".NETFramework,Version=v4.5.2" could not be found, then try invoking ```msbuild```.
4. In order to run tests
  * Go to Neo4j.Driver.Tests folder and invoke ```dotnet xunit```. This will build any dependencies and run unit tests for each of the targets (.net framework 4.5.2 and .net core application 1.0). If you get build errors with the same error as in step 3, you can skip building phase with ```dotnet xunit -nobuild```.
  * Go to Neo4j.Driver.IntegrationTests folder and invoke ```dotnet xunit```. This will build any dependencies and run integration tests for each of the targets (.net framework 4.5.2 and .net core application 1.0). If you get build errors with the same error as in step 3, you can skip building phase with ```dotnet xunit -nobuild```.
  * Go to Neo4j.Driver.Tck.Tests folder and invoke ```dotnet xunit```. This will build any dependencies and run Tck tests for .net framework 4.5.2. If you get build errors with the same error as in step 3, you can skip building phase with ```dotnet xunit -nobuild```.

## MacOS

On MacOS, we develop using the latest Rider release from Jetbrains. You should have [Mono](http://www.mono-project.com/download/#download-lin) and [.NET Core](https://www.microsoft.com/net/download/core) installed on your local machine as per instructed on their corresponding sites.

**_TBC_**

# Cross Platform Support

Although Neo4j.Driver package has been targeting .NET Standard 1.3 for a long time (thus being runnable on several platforms), with latest developments on Visual Studio, Rider and cross platform support on .NET platform, we are now developing this specific driver across different platforms (across Windows, Linux and MacOS).

This document describes what is required on your side to enable development on your platform of choice.

As of 1.5.0 release, the driver itself is targeting .NET Framework 4.5.2 and [.NET Standard 1.3](https://github.com/dotnet/standard/blob/master/docs/versions.md). The unit and integration tests are targeting both .NET Framework 4.5.2 and .NET Core App 1.0, where our TCK tests only support .NET Framework 4.5.2 because of its dependencies.

## Windows

Developing on Windows is straight forward with an installation of Visual Studio 2017 on your machine.

## Linux

On Linux, we develop using the latest Rider release from Jetbrains. You should have [Mono](http://www.mono-project.com/download/#download-lin) and [.NET Core](https://www.microsoft.com/net/download/core) installed on your local machine as per instructed on their corresponding sites.

### IDE

Open the solution file (Neo4j.Driver.sln) with Rider and build the solution (at this stage Rider may ask you to update your settings such as msbuild and framework support). You should have a successful build after completing these steps and be able to run all the tests from within Rider.

### No IDE



## MacOS

On MacOS, we develop using the latest Rider release from Jetbrains. You should have [Mono](http://www.mono-project.com/download/#download-lin) and [.NET Core](https://www.microsoft.com/net/download/core) installed on your local machine as per instructed on their corresponding sites.
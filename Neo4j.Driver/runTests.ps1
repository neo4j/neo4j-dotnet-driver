If ($args.Length -ne 0)
{
	$env:NEORUN_START_ARGS="$args"
	echo $Env:NEORUN_START_ARGS
}

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

iex "$dir\packages\xunit.runner.console.2.1.0\tools\xunit.console.exe $dir\Neo4j.Driver.Tests\bin\Debug\Neo4j.Driver.Tests.dll $dir\Neo4j.Driver.IntegrationTests\bin\Debug\Neo4j.Driver.IntegrationTests.dll $dir\Neo4j.Driver.Tck.Tests\bin\Debug\Neo4j.Driver.Tck.Tests.dll -parallel none"
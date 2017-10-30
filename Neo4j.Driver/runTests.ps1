If ($args.Length -ne 0)
{
	$env:NeoctrlArgs="$args"
	echo $Env:NeoctrlArgs
}
$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

Invoke-Expression "cd $dir\Neo4j.Driver.Tests; dotnet xunit -f net452 -nobuild"
Invoke-Expression "cd $dir\Neo4j.Driver.IntegrationTests; dotnet xunit -f net452 -nobuild -parallel none"
Invoke-Expression "cd $dir\Neo4j.Driver.Tck.Tests; dotnet xunit -f net452 -nobuild -parallel none"


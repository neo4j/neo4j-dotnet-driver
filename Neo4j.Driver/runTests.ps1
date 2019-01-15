If ($args.Length -ne 0)
{
	$env:NeoctrlArgs="$args"
	echo $Env:NeoctrlArgs
}
$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

If (Test-Path $dir\Target) {
	Remove-Item -Path $dir\Target -Recurse -Force	
}

If (Test-Path $dir\..\Target) {
	Remove-Item -Path $dir\..\Target -Recurse -Force	
}

Invoke-Expression "cd $dir\Neo4j.Driver.Tests; dotnet xunit -f net452 -nobuild"
Invoke-Expression "cd $dir\Neo4j.Driver.IntegrationTests; dotnet xunit -f net452 -nobuild -parallel none"


Param( 
    [Parameter(Mandatory=$False,Position=1)]
    [string]$ServerVersion,
    [Parameter(Mandatory=$False,Position=2)]
    [string]$Framework
)

If ($ServerVersion -ne '') 
{
	$env:NEOCTRLARGS="$ServerVersion"
}

If ($Framework -eq '')
{
	$Framework="net46"
}

$RootDir = Split-Path -parent $PSCommandPath
If (Test-Path $RootDir\..\Target) {
	Remove-Item -Path $RootDir\..\Target -Recurse -Force
}

Invoke-Expression "pushd $RootDir\Neo4j.Driver.Tests; dotnet test -f $Framework --no-build; popd"
Invoke-Expression "pushd $RootDir\Neo4j.Driver.IntegrationTests; dotnet test -f $Framework --no-build; popd"


Param( 
    [Parameter(Mandatory=$False)]
    [string]$ServerVersion,
    [Parameter(Mandatory=$False)]
    [string]$Framework
)

Function Write-TeamCity($Text) 
{
    If ($env:TEAMCITY_PROJECT_NAME) 
    {
        Echo $Text
    } 
}

$RootDir = Split-Path -parent $PSCommandPath

$ErrorCode = 0
try
{
    # Clean-up previous downloaded artifacts
    If (Test-Path $RootDir\..\Target) {
        Remove-Item -Path $RootDir\..\Target -Recurse -Force
    }

    $ServerVersionText = 'default'
    If ($ServerVersion -ne '') 
    {
        $ServerVersionText = "$ServerVersion"
        $env:NEOCTRLARGS = "$ServerVersion"
    }
    $TestArgs = ''
    If ($Framework -ne '')
    {
        $TestArgs += "--framework $Framework"
    }

    Write-TeamCity "##teamcity[progressStart 'Running dotnet restore ($ServerVersionText)']"
    Invoke-Expression "pushd $RootDir\..\Neo4j.Driver; dotnet restore; popd" -ErrorAction Stop
    If ($LastExitCode -ne 0) {
        throw "dotnet restore failed"
    }
    Write-TeamCity "##teamcity[progressFinish 'Completed dotnet restore ($ServerVersionText)']"

    Write-TeamCity "##teamcity[progressStart 'Running unit tests ($ServerVersionText)']"
    Invoke-Expression "pushd $RootDir\..\Neo4j.Driver\Neo4j.Driver.Tests; dotnet test $TestArgs; popd" -ErrorAction Stop
    If ($LastExitCode -ne 0) {
        throw "unit tests failed"
    }
    Write-TeamCity "##teamcity[progressFinish 'Completed unit tests ($ServerVersionText)']"

    Write-TeamCity "##teamcity[progressStart 'Running integration tests ($ServerVersionText)']"
    Invoke-Expression "pushd $RootDir\..\Neo4j.Driver\Neo4j.Driver.IntegrationTests; dotnet test $TestArgs; popd" -ErrorAction Stop
    If ($LastExitCode -ne 0) {
        throw "integration tests failed"
    }
    Write-TeamCity "##teamcity[progressFinish 'Completed integration tests ($ServerVersionText)']"
} 
catch
{
    $ErrorCode = 1
    Write-TeamCity "##teamcity[buildStatus status='FAILURE' text='$_']"
}
finally
{
    Invoke-Expression "dotnet build-server shutdown" -ErrorAction Ignore
}

Exit $ErrorCode

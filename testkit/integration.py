
"""
Executed in dotnet driver container.
Responsible for running unit tests.
Assumes driver has been setup by build script prior to this.
"""
import os, subprocess

global failFlag

def run(args):
    try:
        subprocess.run(args, universal_newlines=True, stderr=subprocess.STDOUT, check=True)
    except Exception :
        failedFlag = True



if __name__ == "__main__":

    # run the dotnet integration test framework from the neo4j.driver/neo4j.driver.tests.integration directory.
    # this should not be required once all tests are moved over...
    wd = os.getcwd()
    os.chdir("Neo4j.Driver/Neo4j.Driver.Tests.Integration")

    # This generates a bit ugly output when not in TeamCity, it can be fixed by checking the TEST_IN_TEAMCITY
    # environment flag...(but needs to be passed to the container somehow)
    os.environ.update({"TEAMCITY_PROJECT_NAME": "integrationtests"})

    failFlag = False
    run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~IntegrationTests.Internals"])
    run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~IntegrationTests.Direct"])
    run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~IntegrationTests.Reactive"])
    # TODO: Re-enable for cluster tests if not replaced by testkit native ones.
    # run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~IntegrationTests.Routing"])
    run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~IntegrationTests.Types"])
    run(["dotnet", "test", "Neo4j.Driver.Tests.Integration.csproj", "--filter", "DisplayName~Examples"])

    os.chdir(wd)

    if failFlag:
        os.exit(1)


"""
Executed in dotnet driver container.
Responsible for running unit tests.
Assumes driver has been setup by build script prior to this.
"""
import os, subprocess


def run(args):
    subprocess.run(
        args, universal_newlines=True, stderr=subprocess.STDOUT, check=True)

if __name__ == "__main__":

    # run the dotnet test framework from the neo4j.driver/neo4j.driver.tests directory.
    wd = os.getcwd()
    os.chdir("Neo4j.Driver/Neo4j.Driver.Tests")
    # This generates a bit ugly output when not in TeamCity, it can be fixed by checking the TEST_IN_TEAMCITY
    # environment flag...(but needs to be passed to the container somehow)
    os.environ.update({"TEAMCITY_PROJECT_NAME":"unittests"})
    run(["dotnet", "test", "Neo4j.Driver.Tests.csproj"])
    os.chdir(wd)


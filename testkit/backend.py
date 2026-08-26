"""
Executed in dotnet driver container.
Assumes driver and backend has been built.
Responsible for starting the test backend.
"""

import os, subprocess, sys


if __name__ == "__main__":
    backend_path = os.path.join(
        "bin", "Publish", "Neo4j.Driver.TestKitBackend.dll"
    )

    subprocess.check_call(
        ["dotnet", backend_path],
        stdout=sys.stdout, stderr=sys.stderr
    )


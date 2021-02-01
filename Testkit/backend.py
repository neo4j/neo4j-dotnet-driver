"""
Executed in dotnet driver container.
Assumes driver and backend has been built.
Responsible for starting the test backend.
"""
import os, subprocess

if __name__ == "__main__":
    backend_path = os.path.join("bin", "Publish", "Neo4j.Driver.Tests.TestBackend.dll")
    logfile_path = os.path.join("..", "artifacts", "backend.log")
    err = open("/artifacts/backenderr.log", "w")
    out = open("/artifacts/backendout.log", "w")
    subprocess.check_call(["dotnet", backend_path, "0.0.0.0", "9876", logfile_path], stdout=out, stderr=err)

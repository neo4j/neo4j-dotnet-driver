import os
import subprocess


if __name__ == "__main__":
    uri = "%s://%s:%s" % (
            os.environ["TEST_NEO4J_SCHEME"],
            os.environ["TEST_NEO4J_HOST"],
            os.environ["TEST_NEO4J_PORT"])
    user = os.environ["TEST_NEO4J_USER"]
    password = os.environ["TEST_NEO4J_PASS"]
    is_cluster = os.environ.get("TEST_NEO4J_IS_CLUSTER", False)
    os.environ['NEO4J_USER'] = user
    os.environ['NEO4J_PASSWORD'] = password
    os.environ['NEO4J_URI'] = uri

    cmd = [
        "dotnet",
        "test",
        "--no-restore",
        "--no-build"
    ]

    if os.environ.get("TEST_NEO4J_IS_CLUSTER"):
        cmd.append("--filter")
        cmd.append("DisplayName~CausalClusterStressTests")
    else:
        cmd.append("--filter")
        cmd.append("DisplayName~SingleInstanceStressTests")

    subprocess.run(cmd, universal_newlines=True,
                   stderr=subprocess.STDOUT, check=True,
                   cwd="Neo4j.Driver/Neo4j.Driver.Tests.Integration")

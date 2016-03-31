namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     Factory for creating <see cref="INeo4jInstaller"/> instances
    ///     based on which OS platform the tests are run on.
    /// </summary>
    public static class Neo4jInstallerFactory
    {
        /// <summary>
        ///     Returns an <see cref="INeo4jInstaller"/> instance.
        /// </summary>
        /// <returns>
        ///     Returns an <see cref="INeo4jInstaller"/> instance.
        /// </returns>
        public static INeo4jInstaller Create()
        {
#if __MonoCS__
            return new UnixProcessBasedNeo4jInstaller();
#else
            return new WindowsNeo4jInstaller();
#endif
        }
    }
}

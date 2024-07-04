namespace Neo4j.Driver.Internal.Result;

internal class DatabaseInfo : IDatabaseInfo
{
    public DatabaseInfo()
        : this(null)
    {
    }

    public DatabaseInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    private bool Equals(IDatabaseInfo other)
    {
        return Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((DatabaseInfo)obj);
    }

    public override int GetHashCode()
    {
        return Name != null ? Name.GetHashCode() : 0;
    }
}
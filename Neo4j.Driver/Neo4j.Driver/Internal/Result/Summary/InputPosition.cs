using System;

namespace Neo4j.Driver.Internal.Result;

internal class InputPosition : IInputPosition
{
    protected bool Equals(InputPosition other)
    {
        return Offset == other.Offset && Line == other.Line && Column == other.Column;
    }

    /// <inheritdoc />
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

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((InputPosition)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + Offset.GetHashCode();
            hash = hash * 23 + Line.GetHashCode();
            hash = hash * 23 + Column.GetHashCode();
            return hash;
        }
    }

    public InputPosition(int offset, int line, int column)
    {
        Offset = offset;
        Line = line;
        Column = column;
    }

    public int Offset { get; }
    public int Line { get; }
    public int Column { get; }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Offset)}={Offset}, " +
            $"{nameof(Line)}={Line}, " +
            $"{nameof(Column)}={Column}}}";
    }
}

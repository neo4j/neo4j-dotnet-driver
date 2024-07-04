namespace Neo4j.Driver.Internal.Result;

internal class InputPosition : IInputPosition
{
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
using System;

public class DriverExceptionWrapper : Exception
{
    public DriverExceptionWrapper()
    {
    }

    public DriverExceptionWrapper(Exception inner) : base(null, inner)
    {
    }
}

using System;

namespace Neo4j.Driver.Exceptions
{
    public static class Throw
    {
        public static class ArgumentNullException
        {
            public static void IfNull(object parameter, string paramName)
            {
                If(() => parameter == null, paramName);
            }

            public static void If(Func<bool> func, string paramName)
            {
                if (func())
                {
                    throw new System.ArgumentNullException(paramName);
                }
            }
        }
    }
}
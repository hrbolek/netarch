using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace WebAppArchitecture.Base.Filtering;

public sealed class HasuraFilterException : Exception
{
    public HasuraFilterException(string message)
        : base(message)
    {
    }

    public HasuraFilterException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

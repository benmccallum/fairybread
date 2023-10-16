namespace FairyBread;

[GraphQLName("ValidationError")]
public class DefaultValidationError
{
    private DefaultValidationError(
        string message)
    {
        Message = "A validation error occurred";
        // TODO: Complete a nice rich implementation.
    }

    private DefaultValidationError(
        DefaultValidationException exception)
    {
        Message = "A validation error occurred";
        // TODO: Complete a nice rich implementation.
    }

    public static DefaultValidationError CreateErrorFrom(
        DefaultValidationException ex)
    {
        return new(ex);
    }

    public static DefaultValidationError CreateErrorFrom(
        ArgumentValidationResult argValRes)
    {
        return new("");
    }

    public string Message { get; }
}

public class DefaultValidationException : Exception
{
    public DefaultValidationException(
        IMiddlewareContext context,
        IEnumerable<ArgumentValidationResult> invalidResults)
    {
        Context = context;
        InvalidResults = invalidResults;
    }

    public IMiddlewareContext Context { get; }
    public IEnumerable<ArgumentValidationResult> InvalidResults { get; }
}

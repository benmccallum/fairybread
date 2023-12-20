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

    public static DefaultValidationError CreateErrorFrom(
        ArgumentValidationResult argValRes)
    {
        return new("");
    }

    public string Message { get; }
}
